// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.MakeStructMemberReadOnly;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
internal sealed class CSharpMakeStructMemberReadOnlyDiagnosticAnalyzer()
    : AbstractBuiltInCodeStyleDiagnosticAnalyzer(
        IDEDiagnosticIds.MakeStructMemberReadOnlyDiagnosticId,
        EnforceOnBuildValues.MakeStructMemberReadOnly,
        CSharpCodeStyleOptions.PreferReadOnlyStructMember,
        new LocalizableResourceString(nameof(CSharpAnalyzersResources.Make_member_readonly), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)),
        new LocalizableResourceString(nameof(CSharpAnalyzersResources.Member_can_be_made_readonly), CSharpAnalyzersResources.ResourceManager, typeof(CSharpAnalyzersResources)))
{
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
                if (!ShouldAnalyze(context, out var option))
                    return;

                var methodToDiagnostic = PooledDictionary<IMethodSymbol, Diagnostic>.GetInstance();

                context.RegisterOperationBlockAction(
                    context => AnalyzeBlock(context, option.Notification, methodToDiagnostic));

                context.RegisterSymbolEndAction(
                    context => ProcessResults(context, option.Notification.Severity, methodToDiagnostic));
            }, SymbolKind.NamedType);

            bool ShouldAnalyze(SymbolStartAnalysisContext context, [NotNullWhen(true)] out CodeStyleOption2<bool>? option)
            {
                // Only run on non-readonly structs.  If the struct is already readonly, no need to make the members readonly.
                if (context.Symbol is not INamedTypeSymbol
                    {
                        TypeKind: TypeKind.Struct,
                        IsReadOnly: false,
                        DeclaringSyntaxReferences: [var reference, ..],
                    } structType)
                {
                    option = null;
                    return false;
                }

                var cancellationToken = context.CancellationToken;
                var declaration = reference.GetSyntax(cancellationToken);
                var options = context.GetCSharpAnalyzerOptions(declaration.SyntaxTree);
                option = options.PreferReadOnlyStructMember;
                if (!option.Value || ShouldSkipAnalysis(declaration.SyntaxTree, context.Options, context.Compilation.Options, option.Notification, cancellationToken))
                    return false;

                // Skip analysis if the analysis filter span does not contain the primary location where we would report a diagnostic.
                if (context.FilterSpan is not null)
                {
                    Contract.ThrowIfNull(context.FilterTree);
                    var shouldAnalyze = false;
                    foreach (var member in structType.GetMembers())
                    {
                        if (member is not IMethodSymbol method)
                            continue;

                        var (location, _) = GetDiagnosticLocation(method, cancellationToken);
                        if (location != null && context.ShouldAnalyzeLocation(location))
                        {
                            shouldAnalyze = true;
                            break;
                        }
                    }

                    if (!shouldAnalyze)
                        return false;
                }

                return true;
            }

            void ProcessResults(
                SymbolAnalysisContext context, ReportDiagnostic severity, PooledDictionary<IMethodSymbol, Diagnostic> methodToDiagnostic)
            {
                var cancellationToken = context.CancellationToken;

                // No need to lock the dictionary here.  Processing only is called once, after all mutation work is done.
                foreach (var (method, diagnostic) in methodToDiagnostic)
                {
                    if (method.IsInitOnly && method.AssociatedSymbol is IPropertySymbol owningProperty)
                    {
                        // Iff we have an init method that we want to mark as readonly, we can only do so if there is no
                        // `get` accessor, or if the `get` method is already `readonly` or would determined we want to
                        // mark as `readonly`.
                        var getMethodIsReadOnly =
                            owningProperty.GetMethod is null ||
                            owningProperty.GetMethod.IsReadOnly ||
                            methodToDiagnostic.ContainsKey(owningProperty.GetMethod);

                        // Skip marking this property as readonly for this init method if it would conflict with the get method.
                        if (!getMethodIsReadOnly)
                            continue;
                    }

                    // normal case
                    context.ReportDiagnostic(diagnostic);
                }

                methodToDiagnostic.Free();
            }
        });

    private void AnalyzeBlock(
        OperationBlockAnalysisContext context,
        NotificationOption2 notificationOption,
        Dictionary<IMethodSymbol, Diagnostic> methodToDiagnostic)
    {
        var cancellationToken = context.CancellationToken;

        if (context.OwningSymbol is not IMethodSymbol owningMethod)
            return;

        var (location, additionalLocation) = GetDiagnosticLocation(owningMethod, cancellationToken);
        if (location == null || !context.ShouldAnalyzeSpan(location.SourceSpan))
            return;

        foreach (var blockOperation in context.OperationBlocks)
        {
            // If we have a trivial method that is just `{ throw ... }` or `=> throw ...`, then do not bother
            // analyzing/reporting that it could be made 'readonly'.  These members are likely just being written (or
            // have been generated) and spamming the user with notifications to go change these all is unhelpful.
            if (blockOperation is IBlockOperation { Operations: [IThrowOperation or IExpressionStatementOperation { Operation: IThrowOperation }] })
                return;

            if (BlockOperationPotentiallyMutatesThis(owningMethod, blockOperation, cancellationToken))
                return;
        }

        // Called concurrently.  Make sure we write to this dictionary safely.
        lock (methodToDiagnostic)
        {
            methodToDiagnostic[owningMethod] = DiagnosticHelper.Create(
                Descriptor,
                location,
                notificationOption,
                additionalLocations: ImmutableArray.Create(additionalLocation),
                properties: null);
        }
    }

    private static (Location? location, Location? additionalLocation) GetDiagnosticLocation(
        IMethodSymbol owningMethod,
        CancellationToken cancellationToken)
    {
        // if it's not a method, or it's already readonly, nothing to do.
        if (owningMethod.MethodKind is not (MethodKind.Ordinary or MethodKind.ExplicitInterfaceImplementation or MethodKind.PropertyGet or MethodKind.PropertySet)
            || owningMethod.IsReadOnly
            || owningMethod.IsStatic
            || owningMethod.IsImplicitlyDeclared)
        {
            return default;
        }

        // An init accessor in a readonly property is already readonly.  No need to analyze it.  Note: there is no way
        // to tell this symbolically.  We have to check to the syntax here.
        if (owningMethod.IsInitOnly &&
            owningMethod.AssociatedSymbol is IPropertySymbol { DeclaringSyntaxReferences: [var reference, ..] } &&
            reference.GetSyntax(cancellationToken) is PropertyDeclarationSyntax property &&
            property.Modifiers.Any(SyntaxKind.ReadOnlyKeyword))
        {
            return default;
        }

        var methodReference = owningMethod.DeclaringSyntaxReferences[0];
        var declaration = methodReference.GetSyntax(cancellationToken);

        var nameToken = declaration switch
        {
            MethodDeclarationSyntax methodDeclaration => methodDeclaration.Identifier,
            AccessorDeclarationSyntax accessorDeclaration => accessorDeclaration.Keyword,
            PropertyDeclarationSyntax propertyDeclaration => propertyDeclaration.Identifier,
            IndexerDeclarationSyntax indexerDeclaration => indexerDeclaration.ThisKeyword,
            ArrowExpressionClauseSyntax arrowExpression => arrowExpression.ArrowToken,
            _ => (SyntaxToken?)null
        };

        if (declaration is ArrowExpressionClauseSyntax)
            declaration = declaration.GetRequiredParent();

        if (nameToken is null)
            return default;

        return (nameToken.Value.GetLocation(), declaration.GetLocation());
    }

    private static bool BlockOperationPotentiallyMutatesThis(
        IMethodSymbol owningMethod,
        IOperation blockOperation,
        CancellationToken cancellationToken)
    {
        var semanticModel = blockOperation.SemanticModel;
        Contract.ThrowIfNull(semanticModel);
        foreach (var operation in blockOperation.DescendantsAndSelf())
        {
            // Do not suggest making containing method readonly until we have full understanding of it.
            if (operation is IInvalidOperation)
                return true;

            if (ReferencesThisInstance(operation, cancellationToken) &&
                OperationPotentiallyMutatesThis(semanticModel, owningMethod, operation, cancellationToken))
            {
                return true;
            }
        }

        return false;

        static bool ReferencesThisInstance(IOperation operation, CancellationToken cancellationToken)
        {
            // An actual usage of `this` or `base` in the code.
            if (operation is IInstanceReferenceOperation)
                return true;

            // A primary constructor parameter implicitly references 'this' instance.
            if (operation is IParameterReferenceOperation { Parameter: var parameter } &&
                parameter.IsPrimaryConstructor(cancellationToken))
            {
                return true;
            }

            return false;
        }
    }

    private static bool IsPotentiallyValueType(IOperation? instance)
    {
        // 1. A struct is a value type.
        // 2. A type parameter that does not have the explicit 'class' constraint is potentially a value type.
        return instance is { Type.TypeKind: TypeKind.Struct } ||
               instance is { Type: ITypeParameterSymbol { HasReferenceTypeConstraint: false } };
    }

    private static bool OperationPotentiallyMutatesThis(
        SemanticModel semanticModel,
        IMethodSymbol owningMethod,
        IOperation instanceOperation,
        CancellationToken cancellationToken)
    {
        // if we have an explicit 'this' in code, and we're overwriting it directly (e.g. `ref this` or `this = ...`
        // then can't make this `readonly`.
        if (!instanceOperation.IsImplicit &&
            CSharpSemanticFacts.Instance.IsWrittenTo(semanticModel, instanceOperation.Syntax, cancellationToken))
        {
            return true;
        }

        // We only care if operation is a value type when looking at if it is somehow mutated with the operations that
        // are performed on it.  In other words.  `valueType.X = 0` is not allowed while `referenceType.X = 0` is fine
        // (since the former actually mutates storage in 'this' which would prevent this method from becoming readonly.
        if (!IsPotentiallyValueType(instanceOperation))
            return false;

        // Now walk up the instance-operation and see if any operation actually or potentially mutates this value.
        for (var operation = instanceOperation.Parent; operation != null; operation = operation.Parent)
        {
            // Had a parent we didn't understand.  Assume that 'this' could be mutated.
            if (operation.Kind == OperationKind.None)
                return true;

            if (operation is IFieldReferenceOperation { Field.IsReadOnly: false } fieldReference &&
                IsPotentiallyValueType(fieldReference.Instance))
            {
                // If we're writing to a field off of 'this'.  Can't make this `readonly`.
                if (CSharpSemanticFacts.Instance.IsWrittenTo(semanticModel, fieldReference.Syntax, cancellationToken))
                    return true;

                // otherwise, keeping walking upwards to make sure subsequent accesses off this field are ok.
                continue;
            }

            if (operation is IPropertyReferenceOperation propertyReference &&
                IsPotentiallyValueType(propertyReference.Instance))
            {
                // If we're writing to a prop off of 'this'.  Can't make this `readonly`.
                if (CSharpSemanticFacts.Instance.IsWrittenTo(semanticModel, propertyReference.Syntax, cancellationToken))
                    return true;

                // If we're reading, that's only ok if we know the get-accessor exists and it is itself readonly.
                // Otherwise it could mutate the value.
                if (propertyReference.Property.GetMethod is null ||
                    !propertyReference.Property.GetMethod.IsReadOnly)
                {
                    return true;
                }

                // a safe property reference.  Can stop looking upwards as this cannot return a value that could
                // mutate this value.
                return false;
            }

            if (operation is IEventReferenceOperation eventReference &&
                IsPotentiallyValueType(eventReference.Instance))
            {
                // += or -= on an event off of a struct will cause a copy if we become readonly.
                if (operation.Parent is IEventAssignmentOperation)
                    return true;

                // a safe event reference.  Can stop looking upwards as this cannot return a value that could
                // mutate this value.
                return false;
            }

            if (operation is IInlineArrayAccessOperation)
            {
                // If we're writing into an inline-array off of 'this'.  Then we can't make this `readonly`.
                if (CSharpSemanticFacts.Instance.IsWrittenTo(semanticModel, operation.Syntax, cancellationToken))
                    return true;

                // We're reading a value from inside the inline-array.  Have to keep looking upwards to see how the
                // value is treated.
                continue;
            }

            // See if we're accessing or invoking a method.
            if (operation is IMethodReferenceOperation methodRefOperation)
            {
                // Either a mutating or not mutating method reference.  Regardless, once we examine it, we're done
                // looking up as the method itself cannot return anything that could mutate this.
                return IsPotentiallyMutatingMethod(owningMethod, methodRefOperation.Instance, methodRefOperation.Method);
            }

            if (operation is IInvocationOperation invocationOperation)
            {
                // Either a mutating or not mutating method reference.  Regardless, once we examine it, we're done
                // looking up as the method itself cannot return anything that could mutate this.
                return IsPotentiallyMutatingMethod(owningMethod, invocationOperation.Instance, invocationOperation.TargetMethod);
            }

            // Converting an inline-array into a Span<T> allows the array to be written into.  As such, we have to
            // consider this a potential future mutation of 'this'.
            if (operation is IConversionOperation conversionOperation)
            {
                var conversion = conversionOperation.GetConversion();
                if (conversion.IsInlineArray && conversionOperation.Type.IsSpan())
                    return true;
            }

            // Wasn't something that mutates this instance.  Go onto the next instance expression.
            break;
        }

        return false;
    }

    private static bool IsPotentiallyMutatingMethod(
        IMethodSymbol owningMethod,
        IOperation? instance,
        IMethodSymbol methodReference)
    {
        if (!IsPotentiallyValueType(instance))
            return false;

        // Calling a readonly method off of a struct is fine since we know it can't mutate.
        if (methodReference.IsReadOnly)
            return false;

        // If we're referencing the method we're in (Which isn't readonly yet) we don't want to mark us as
        // not-readonly. a recursive call shouldn't impact the final result.
        if (methodReference.Equals(owningMethod))
            return false;

        // Any methods from System.Object or System.ValueType called on this `this` don't stop this from being readonly.
        if (methodReference.ContainingType.SpecialType is SpecialType.System_Object or SpecialType.System_ValueType)
            return false;

        // Any other non-readonly method usage on this means we can't be readonly.
        return true;
    }
}
