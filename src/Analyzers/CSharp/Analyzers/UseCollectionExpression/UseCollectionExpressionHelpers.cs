// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.UseCollectionExpression;
using Microsoft.CodeAnalysis.UseCollectionInitializer;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

using static CSharpSyntaxTokens;
using static SyntaxFactory;

internal static class UseCollectionExpressionHelpers
{
    public const string UnwrapArgument = nameof(UnwrapArgument);
    public const string UseSpread = nameof(UseSpread);

    private static readonly CollectionExpressionSyntax s_emptyCollectionExpression = CollectionExpression();

    /// <summary>
    /// Set of type-names that are blocked from moving over to collection expressions because the semantics of them are
    /// known to be specialized, and thus could change semantics in undesirable ways if the compiler emitted its own
    /// code as an replacement.
    /// </summary>
    public static readonly ImmutableHashSet<string?> BannedTypes = [
        nameof(ParallelEnumerable),
        nameof(ParallelQuery),
        // Special internal runtime interface that is optimized for fast path conversions of collections.
        "IIListProvider"];

    private static readonly SymbolEquivalenceComparer s_tupleNamesCanDifferComparer = SymbolEquivalenceComparer.Create(
        // Not relevant.  We are not comparing method signatures.
        distinguishRefFromOut: true,
        // Not relevant.  We are not comparing method signatures.
        objectAndDynamicCompareEqually: false,
        // Not relevant.  We are not comparing method signatures.
        arrayAndReadOnlySpanCompareEqually: false,
        // The value we're tweaking.
        tupleNamesMustMatch: false,
        // We do not want to ignore this.  `ImmutableArray<string?>` should not be convertible to `ImmutableArray<string>`
        ignoreNullableAnnotations: false);

    private static readonly SymbolEquivalenceComparer s_arrayAndReadOnlySpanCompareEquallyComparer = s_tupleNamesCanDifferComparer.With(arrayAndReadOnlySpanCompareEqually: true);

    public static bool CanReplaceWithCollectionExpression(
        SemanticModel semanticModel,
        ExpressionSyntax expression,
        INamedTypeSymbol? expressionType,
        bool isSingletonInstance,
        bool allowSemanticsChange,
        bool skipVerificationForReplacedNode,
        CancellationToken cancellationToken,
        out bool changesSemantics)
    {
        // To keep things simple, all we do is replace the existing expression with the `[]` literal.This is an
        // 'untyped' collection expression literal, so it tells us if the new code will have any issues moving to
        // something untyped.  This will also tell us if we have any ambiguities (because there are multiple destination
        // types that could accept the collection expression).
        return CanReplaceWithCollectionExpression(
            semanticModel, expression, s_emptyCollectionExpression, expressionType, isSingletonInstance, allowSemanticsChange, skipVerificationForReplacedNode, cancellationToken, out changesSemantics);
    }

    public static bool CanReplaceWithCollectionExpression(
        SemanticModel semanticModel,
        ExpressionSyntax expression,
        CollectionExpressionSyntax replacementExpression,
        INamedTypeSymbol? expressionType,
        bool isSingletonInstance,
        bool allowSemanticsChange,
        bool skipVerificationForReplacedNode,
        CancellationToken cancellationToken,
        out bool changesSemantics)
    {
        var compilation = semanticModel.Compilation;
        changesSemantics = false;

        var topMostExpression = expression.WalkUpParentheses();
        if (topMostExpression.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
            return false;

        var parent = topMostExpression.GetRequiredParent();

        var targetType = topMostExpression.GetTargetType(semanticModel, cancellationToken);
        if (targetType is null or IErrorTypeSymbol)
            return false;

        // (X[])[1, 2, 3] is target typed.  `(X)[1, 2, 3]` is currently not (because it looks like indexing into an expr).
        if (topMostExpression.Parent is CastExpressionSyntax { Type: IdentifierNameSyntax })
            return false;

        // X[] = new Y[] { 1, 2, 3 }
        //
        // First, we don't change things if X and Y are different.  That could lead to something observable at
        // runtime in the case of something like:  object[] x = new string[] ...

        var originalTypeInfo = semanticModel.GetTypeInfo(topMostExpression, cancellationToken);
        if (originalTypeInfo.Type is IErrorTypeSymbol)
            return false;

        if (originalTypeInfo.ConvertedType is null or IErrorTypeSymbol)
            return false;

        if (!IsConstructibleCollectionType(compilation, originalTypeInfo.ConvertedType.OriginalDefinition))
            return false;

        if (expression.IsInExpressionTree(semanticModel, expressionType, cancellationToken))
            return false;

        // Conservatively, avoid making this change if the original expression was itself converted. Consider, for
        // example, `IEnumerable<string> x = new List<string>()`.  If we change that to `[]` we will still compile,
        // but it's possible we'll end up with different types at runtime that may cause problems.
        //
        // Note: we can relax this on a case by case basis if we feel like it's acceptable.
        if (originalTypeInfo.Type != null &&
            !originalTypeInfo.Type.Equals(originalTypeInfo.ConvertedType) &&
            !IsSafeConversionWhenTypesDoNotMatch(out changesSemantics))
        {
            return false;
        }

        var operation = semanticModel.GetOperation(topMostExpression, cancellationToken);
        if (operation?.Parent is IAssignmentOperation { Type.TypeKind: TypeKind.Dynamic })
            return false;

        // HACK: Workaround lack of compiler information for collection expression conversions with casts.
        // Specifically, hardcode in knowledge that a cast to a constructible collection type of the empty collection
        // expression will always succeed, and there's no need to actually validate semantics there.
        // Tracked by https://github.com/dotnet/roslyn/issues/68826
        if (parent is CastExpressionSyntax)
            return IsConstructibleCollectionType(compilation, semanticModel.GetTypeInfo(parent, cancellationToken).Type);

        // Looks good as something to replace.  Now check the semantics of making the replacement to see if there would
        // any issues.
        var speculationAnalyzer = new SpeculationAnalyzer(
            topMostExpression,
            replacementExpression,
            semanticModel,
            cancellationToken,
            skipVerificationForReplacedNode,
            failOnOverloadResolutionFailuresInOriginalCode: true);

        if (speculationAnalyzer.ReplacementChangesSemantics())
            return false;

        // Ensure that we have a collection conversion with the replacement.  If not, this wasn't a legal replacement
        // (for example, we're trying to replace an expression that is converted to something that isn't even a
        // collection type).
        //
        // Note: an identity conversion is always legal without needing any more checks.
        var conversion = speculationAnalyzer.SpeculativeSemanticModel.GetConversion(speculationAnalyzer.ReplacedExpression, cancellationToken);
        if (conversion.IsIdentity)
            return true;

        if (!conversion.IsCollectionExpression)
            return false;

        // The new expression's converted type has to equal the old expressions as well.  Otherwise, we're now
        // converting this to some different collection type unintentionally.
        //
        // Note: it's acceptable to be originally converting to an array, and now converting to a ROS.  This occurs with
        // APIs that started out just taking an array, but which now have an overload that takes a span.  APIs should
        // only do this when the new api has the same semantics (outside of perf), and the language and runtime strongly
        // want code to call the new api.  So it's desirable to change here.
        var replacedTypeInfo = speculationAnalyzer.SpeculativeSemanticModel.GetTypeInfo(speculationAnalyzer.ReplacedExpression, cancellationToken);
        if (!originalTypeInfo.ConvertedType.Equals(replacedTypeInfo.ConvertedType) &&
            !s_arrayAndReadOnlySpanCompareEquallyComparer.Equals(originalTypeInfo.ConvertedType, replacedTypeInfo.ConvertedType))
        {
            return false;
        }

        if (IsImplementationOfCollectionBuilderPattern())
            return false;

        return true;

        bool IsSafeConversionWhenTypesDoNotMatch(out bool changesSemantics)
        {
            changesSemantics = false;
            var type = originalTypeInfo.Type;
            var convertedType = originalTypeInfo.ConvertedType;

            var convertedToReadOnlySpan =
                convertedType.Name == nameof(ReadOnlySpan<>) &&
                convertedType.OriginalDefinition.Equals(compilation.ReadOnlySpanOfTType());

            var convertedToSpan =
                convertedType.Name == nameof(Span<>) &&
                convertedType.OriginalDefinition.Equals(compilation.SpanOfTType());

            // ReadOnlySpan<X> x = stackalloc[] ...
            //
            // This will be a Span<X> converted to a ReadOnlySpan<X>.  This is always safe as ReadOnlySpan is more
            // restrictive than Span<X>
            var isSpanToReadOnlySpan =
                convertedToReadOnlySpan &&
                type.Name == nameof(Span<>) &&
                type.OriginalDefinition.Equals(compilation.SpanOfTType()) &&
                convertedType.GetTypeArguments()[0].Equals(type.GetTypeArguments()[0]);
            if (isSpanToReadOnlySpan)
                return true;

            // ReadOnlySpan<X> x = new X[] ...  or
            // Span<X> x = new X[] ...
            //
            // This may or may not be safe.  If the original 'x' was a local, then it would previously have had global
            // scope (due to the array).  In that case, we have to make sure converting to a collection expression
            // (which would had local scope) will not cause problems.

            if (type is IArrayTypeSymbol arrayType &&
                (convertedToSpan || convertedToReadOnlySpan) &&
                arrayType.ElementType.Equals(convertedType.GetTypeArguments()[0]))
            {
                return IsSafeConversionOfArrayToSpanType(semanticModel, expression, cancellationToken);
            }

            // Allow tuple names to be different.  Because we are target typing the names can be picked up by the target type.
            if (s_tupleNamesCanDifferComparer.Equals(type, convertedType))
                return true;

            // It's always safe to convert List<X> to ICollection<X> or IList<X> as the language guarantees that it will
            // continue emitting a List<X> for those target types.
            var isWellKnownCollectionReadWriteInterface = IsWellKnownCollectionReadWriteInterface(convertedType);
            if (isWellKnownCollectionReadWriteInterface &&
                Equals(type.OriginalDefinition, compilation.ListOfTType()) &&
                type.AllInterfaces.Contains(convertedType))
            {
                return true;
            }

            // Before this point are all the changes that we can detect that are always safe to make.
            if (!allowSemanticsChange)
                return false;

            // After this point are all the changes that we can detect that may change runtime semantics (for example,
            // converting an array into a compiler-generated IEnumerable), but which can be ok since the user has opted
            // into allowing that.
            changesSemantics = true;

            // In the case of a singleton (like `Array.Empty<T>()`) we don't want to convert to `IList<T>` as that
            // will replace the code with code that now always allocates.
            if (isSingletonInstance && isWellKnownCollectionReadWriteInterface)
                return false;

            // Ok to convert in cases like:
            //
            // `IEnumerable<object> obj = Array.Empty<object>();` or
            // `IEnumerable<string> obj = new[] { "" };`
            if (IsWellKnownCollectionInterface(convertedType) && type.AllInterfaces.Contains(convertedType))
            {
                // The observable collections are known to have significantly different behavior than List<T>.  So
                // disallow converting those types to ensure semantics are preserved.  We do this even though
                // allowSemanticsChange is true because this will basically be certain to break semantics, as opposed to
                // the more common case where semantics may change slightly, but likely not in a way that breaks code.
                if (type.Name is nameof(ObservableCollection<>) or nameof(ReadOnlyObservableCollection<>))
                    return false;

                // If the original expression was creating a set, but is being assigned to one of the well known
                // interfaces, then we don't want to convert this.  This is because the set has different semantics than
                // the linear sequence types.
                var isetType = compilation.ISetOfTType();
                var ireadOnlySetType = compilation.IReadOnlySetOfTType();
                if (type.AllInterfaces.Any(t => t.OriginalDefinition.Equals(isetType) || t.OriginalDefinition.Equals(ireadOnlySetType)))
                    return false;

                return true;
            }

            // Implicit reference array conversion is acceptable if the user is ok with semantics changing.  For example:
            //
            // `object[] obj = new[] { "a" }` or
            // `IEnumerable<object> obj = new[] { "a" }` or
            //
            // Before the change this would be a string-array.  With a collection expression this will become an object[].
            if (type is IArrayTypeSymbol)
            {
                var conversion = compilation.ClassifyConversion(type, convertedType);
                if (conversion.IsIdentityOrImplicitReference())
                    return true;
            }

            // Add more cases to support here.
            return false;
        }

        bool IsImplementationOfCollectionBuilderPattern()
        {
            // Check if the type being created has a CollectionBuilder attribute that points to the method we're currently in.
            // If so, suppress the diagnostic to avoid suggesting a change that would cause infinite recursion.
            // For example, if we're inside the Create method of a CollectionBuilder, and we have:
            //   MyCustomCollection<T> collection = new();
            //   foreach (T item in items) { collection.Add(item); }
            // We should NOT suggest changing it to:
            //   MyCustomCollection<T> collection = [.. items];
            // Because that would recursively call the same Create method.

            if (targetType is not INamedTypeSymbol namedType)
                return false;

            // For generic types, get the type definition to check for the attribute
            var typeToCheck = namedType.OriginalDefinition;

            // Look for CollectionBuilder attribute on the type
            var collectionBuilderAttribute = typeToCheck.GetAttributes().FirstOrDefault(attr =>
                attr.AttributeClass?.IsCollectionBuilderAttribute() == true);

            if (collectionBuilderAttribute == null)
                return false;

            // Get the builder type and method name from the attribute.
            // CollectionBuilderAttribute has exactly 2 constructor parameters: builderType and methodName
            if (collectionBuilderAttribute.ConstructorArguments is not
                [
                { Kind: TypedConstantKind.Type, Value: INamedTypeSymbol builderType },
                { Kind: TypedConstantKind.Primitive, Value: string methodName }
                ])
            {
                return false;
            }

            // Get the containing method we're currently analyzing
            var containingMethod = semanticModel.GetEnclosingSymbol<IMethodSymbol>(expression.SpanStart, cancellationToken);
            if (containingMethod == null)
                return false;

            // Check if the containing method matches the CollectionBuilder method
            // We need to compare the original definitions in case the method is generic
            if (containingMethod.Name == methodName &&
                SymbolEqualityComparer.Default.Equals(containingMethod.ContainingType.OriginalDefinition, builderType.OriginalDefinition))
            {
                return true;
            }

            return false;
        }
    }

    public static bool IsWellKnownCollectionInterface(ITypeSymbol type)
        => IsWellKnownCollectionReadOnlyInterface(type) || IsWellKnownCollectionReadWriteInterface(type);

    public static bool IsWellKnownCollectionReadOnlyInterface(ITypeSymbol type)
    {
        return type.OriginalDefinition.SpecialType
            is SpecialType.System_Collections_Generic_IEnumerable_T
            or SpecialType.System_Collections_Generic_IReadOnlyCollection_T
            or SpecialType.System_Collections_Generic_IReadOnlyList_T;
    }

    public static bool IsWellKnownCollectionReadWriteInterface(ITypeSymbol type)
    {
        return type.OriginalDefinition.SpecialType
            is SpecialType.System_Collections_Generic_ICollection_T
            or SpecialType.System_Collections_Generic_IList_T;
    }

    public static bool IsConstructibleCollectionType(Compilation compilation, ITypeSymbol? type)
    {
        if (type is null)
            return false;

        // Arrays are always a valid collection expression type.
        if (type is IArrayTypeSymbol)
            return true;

        // Has to be a real named type at this point.
        if (type is INamedTypeSymbol namedType)
        {
            // Span<T> and ReadOnlySpan<T> are always valid collection expression types.
            if (namedType.OriginalDefinition.Equals(compilation.SpanOfTType()) ||
                namedType.OriginalDefinition.Equals(compilation.ReadOnlySpanOfTType()))
            {
                return true;
            }

            // If it has a [CollectionBuilder] attribute on it, it is a valid collection expression type.
            if (namedType.GetAttributes().Any(a => a.AttributeClass.IsCollectionBuilderAttribute()))
                return true;

            if (IsWellKnownCollectionInterface(namedType))
                return true;

            // At this point, all that is left are collection-initializer types.  These need to derive from
            // System.Collections.IEnumerable, and have an invokable no-arg constructor.

            // Abstract type don't have invokable constructors at all.
            if (namedType.IsAbstract)
                return false;

            if (namedType.AllInterfaces.Contains(compilation.IEnumerableType()!))
            {
                // If they have an accessible `public C(int capacity)` constructor, the lang prefers calling that.
                var constructors = namedType.Constructors;
                var capacityConstructor = GetAccessibleInstanceConstructor(constructors, c => c.Parameters is [{ Name: "capacity", Type.SpecialType: SpecialType.System_Int32 }]);
                if (capacityConstructor != null)
                    return true;

                var noArgConstructor =
                    GetAccessibleInstanceConstructor(constructors, c => c.Parameters.IsEmpty) ??
                    GetAccessibleInstanceConstructor(constructors, c => c.Parameters.All(p => p.IsOptional || p.IsParams));
                if (noArgConstructor != null)
                {
                    // If we have a struct, and the constructor we find is implicitly declared, don't consider this
                    // a constructible type.  It's likely the user would just get the `default` instance of the
                    // collection (like with ImmutableArray<T>) which would then not actually work.  If the struct
                    // does have an explicit constructor though, that's a good sign it can actually be constructed
                    // safely with the no-arg `new S()` call.
                    if (!(namedType.TypeKind == TypeKind.Struct && noArgConstructor.IsImplicitlyDeclared))
                        return true;
                }
            }
        }

        // Anything else is not constructible.
        return false;

        IMethodSymbol? GetAccessibleInstanceConstructor(ImmutableArray<IMethodSymbol> constructors, Func<IMethodSymbol, bool> predicate)
        {
            var constructor = constructors.FirstOrDefault(c => !c.IsStatic && predicate(c));
            return constructor is not null && constructor.IsAccessibleWithin(compilation.Assembly) ? constructor : null;
        }
    }

    private static bool IsSafeConversionOfArrayToSpanType(
        SemanticModel semanticModel, ExpressionSyntax expression, CancellationToken cancellationToken)
    {
        var initializer = expression switch
        {
            ArrayCreationExpressionSyntax arrayCreation => arrayCreation.Initializer,
            ImplicitArrayCreationExpressionSyntax arrayCreation => arrayCreation.Initializer,
            _ => null,
        };

        // First, if the current expression only contains primitive constants, then that's guaranteed to be
        // something the compiler can compile directly into the RVA section of the dll, and as such will
        // stay local scoped when converting to a collection expression.
        if (initializer != null)
        {
            if (initializer.Expressions.All(IsPrimitiveConstant))
                return true;
        }
        else
        {
            // Otherwise, if this is an Array.Empty<T>() invocation or `new X[0]` instantiation, then this is always
            // safe to convert from an array to a span.
            if (IsCollectionEmptyAccess(semanticModel, expression, cancellationToken))
                return true;

            if (expression is ArrayCreationExpressionSyntax { Type: ArrayTypeSyntax { RankSpecifiers: [{ Sizes: [var size] }, ..] } } &&
                semanticModel.GetConstantValue(size, cancellationToken).Value is 0)
            {
                return true;
            }

            // Don't support anything else without an initializer (for now).
            return false;
        }

        // Ok, we have non primitive/constant values.  Moving to a collection expression will make this span have local
        // scope.  Have to make sure that's ok.  We do our analysis in an iterative fashion.  Starting with the original
        // expression and seeing how its scope flows outward (including to other locals).  We will then require that any
        // ref-type values we encounter (including the initial one) cannot flow out of the method we're in.
        //
        // Because we're analyzing the code in multiple passes (until we reach a fixed point), we have to ensure we only
        // examine locals and expressions once.
        using var _1 = ArrayBuilder<ExpressionSyntax>.GetInstance(out var expressionsToProcess);
        using var _2 = PooledHashSet<ExpressionSyntax>.GetInstance(out var seenExpressions);
        using var _3 = PooledHashSet<ILocalSymbol>.GetInstance(out var seenLocals);

        AddExpressionToProcess(expression);

        while (expressionsToProcess.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var locallyScopedExpression = expressionsToProcess.Pop().WalkUpParentheses();

            // Expression used on its own, without its result being used.  Safe to convert.
            if (locallyScopedExpression.Parent is ExpressionStatementSyntax)
                continue;

            // If the expression is returned out, then it definitely has non-local scope and we definitely cannot
            // convert it.
            if (locallyScopedExpression.Parent is ReturnStatementSyntax or ArrowExpressionClauseSyntax)
                return false;

            if (locallyScopedExpression.Parent is ArgumentSyntax argument)
            {
                // if it's passed into something, ensure that that is safe.  Note: this may discover more
                // expressions and variables to test out.
                if (!IsSafeUsageOfSpanAsArgument(argument))
                    return false;

                continue;
            }

            if (locallyScopedExpression.Parent is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Expression == locallyScopedExpression)
            {
                if (memberAccess.Parent is InvocationExpressionSyntax invocationExpression)
                {
                    // something like s.Slice(...).  We're safe if the result of this invocation is safe.
                    if (semanticModel.GetSymbolInfo(invocationExpression, cancellationToken).Symbol is not IMethodSymbol method)
                        return false;

                    if (method.ReturnType.IsRefLikeType)
                        AddExpressionToProcess(invocationExpression);

                    AddRefLikeOutParameters(invocationExpression.ArgumentList, argumentToSkip: null);
                }
                else
                {
                    // just a property access.  Like 's.Length'.  This is safe to convert keep going.
                    var symbol = semanticModel.GetSymbolInfo(memberAccess, cancellationToken).Symbol;
                    if (symbol is not IPropertySymbol and not IFieldSymbol)
                        return false;
                }

                continue;
            }

            if (locallyScopedExpression.Parent is ElementAccessExpressionSyntax elementAccess)
            {
                // Something like s[...].  We're safe if the result of the element access it safe.
                var methodOrProperty = semanticModel.GetSymbolInfo(elementAccess, cancellationToken).Symbol;
                if (methodOrProperty is not IMethodSymbol and not IPropertySymbol)
                    return false;

                if (methodOrProperty.GetMemberType()!.IsRefLikeType)
                    AddExpressionToProcess(elementAccess);

                AddRefLikeOutParameters(elementAccess.ArgumentList, argumentToSkip: null);
                continue;
            }

            if (locallyScopedExpression.Parent is EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax declarator })
            {
                // if it's assigned to a new variable, check that variables for how it is used.
                if (!AddLocalToProcess(declarator))
                    return false;

                continue;
            }

            if (locallyScopedExpression.Parent is AssignmentExpressionSyntax assignment &&
                assignment.Right == locallyScopedExpression)
            {
                // If it's assigned to something on the left, that's only safe if it's another locally scoped symbol.
                var leftSymbol = semanticModel.GetSymbolInfo(assignment.Left, cancellationToken).Symbol;
                if (leftSymbol is not ILocalSymbol { ScopedKind: ScopedKind.ScopedValue })
                    return false;

                continue;
            }

            // Something unsupported.  Can always add new cases here in the future if it can be determined.
            return false;
        }

        // Everything we processed was good.  Can safely convert this global-scoped array to a local scoped span.
        return true;

        void AddExpressionToProcess(ExpressionSyntax expression)
        {
            if (seenExpressions.Add(expression))
                expressionsToProcess.Push(expression);
        }

        bool AddLocalToProcess(SyntaxNode declarator)
        {
            if (semanticModel.GetDeclaredSymbol(declarator, cancellationToken) is not ILocalSymbol local)
                return false;

            // Only process a local once.
            if (!seenLocals.Add(local))
                return true;

            // If the local we're assigning to isn't a ref-type, then scoping isn't relevant, and we don't have to
            // examine it.
            if (!local.Type.IsRefLikeType)
                return true;

            // If the local is already scoped locally, then we don't need to do any additional checks on how it is
            // used.  It's always safe to convert to a locally scoped span.
            if (local.ScopedKind == ScopedKind.ScopedValue)
                return true;

            var containingBlock = declarator.FirstAncestorOrSelf<BlockSyntax>();
            if (containingBlock == null)
                return false;

            foreach (var identifier in containingBlock.DescendantNodes().OfType<IdentifierNameSyntax>())
            {
                if (identifier.Identifier.ValueText != local.Name)
                    continue;

                var symbol = semanticModel.GetSymbolInfo(identifier, cancellationToken).Symbol;
                if (!local.Equals(symbol))
                    continue;

                // Ok, found a reference to the local, add this to the list to process.
                AddExpressionToProcess(identifier);
            }

            return true;
        }

        bool IsSafeUsageOfSpanAsArgument(ArgumentSyntax argument)
        {
            if (argument.Expression.IsNameOfArgumentExpression())
                return true;

            var parameter = argument.DetermineParameter(semanticModel, cancellationToken: cancellationToken);
            if (parameter is null)
                return false;

            // Goo([i]) is always safe if the argument is 'scoped' as this can't escape.
            if (parameter.ScopedKind != ScopedKind.ScopedValue)
            {
                // Ok.  Was passed to something non-scoped.  Check the rest of the signature.
                if (parameter.ContainingSymbol is not IMethodSymbol method)
                    return false;

                // method returns something by-ref.  Have to make sure the entire method call is safe.
                if (argument.Parent is not BaseArgumentListSyntax { Parent: ExpressionSyntax parentInvocation } argumentList)
                    return false;

                if (method.ReturnType.IsRefLikeType)
                    AddExpressionToProcess(parentInvocation);

                // Now check the rest of the arguments.  If there are any out-parameters that are ref-structs,
                // then make sure those are safe as well.
                AddRefLikeOutParameters(argumentList, argument);
            }

            // This should be safe to convert.
            return true;
        }

        bool AddRefLikeOutParameters(BaseArgumentListSyntax argumentList, ArgumentSyntax? argumentToSkip)
        {
            foreach (var siblingArgument in argumentList.Arguments)
            {
                if (siblingArgument != argumentToSkip)
                {
                    var siblingParameter = siblingArgument.DetermineParameter(semanticModel, cancellationToken: cancellationToken);
                    if (siblingParameter is null)
                        return false;

                    if (siblingParameter.Type.IsRefLikeType &&
                        siblingArgument.RefOrOutKeyword.Kind() == SyntaxKind.OutKeyword &&
                        siblingArgument.Expression is DeclarationExpressionSyntax { Designation: SingleVariableDesignationSyntax designation })
                    {
                        // if it's assigned to a new variable, check that variables for how it is used.
                        if (!AddLocalToProcess(designation))
                            return false;
                    }
                }
            }

            return true;
        }

        bool IsPrimitiveConstant(ExpressionSyntax expression)
            => semanticModel.GetConstantValue(expression, cancellationToken).HasValue &&
               semanticModel.GetTypeInfo(expression, cancellationToken).Type?.IsValueType == true;
    }

    public static CollectionExpressionSyntax ConvertInitializerToCollectionExpression(
        InitializerExpressionSyntax initializer, bool wasOnSingleLine)
    {
        // if the initializer is already on multiple lines, keep it that way.  otherwise, squash from `{ 1, 2, 3 }` to `[1, 2, 3]`
        var openBracket = OpenBracketToken.WithTriviaFrom(initializer.OpenBraceToken);
        var elements = initializer.Expressions.GetWithSeparators().SelectAsArray(
            i => i.IsToken ? i : ExpressionElement((ExpressionSyntax)i.AsNode()!));
        var closeBracket = CloseBracketToken.WithTriviaFrom(initializer.CloseBraceToken);

        // If it was on a single line to begin with, then remove the inner spaces on the `{ ... }` to create `[...]`. If
        // it was multiline, leave alone as we want the brackets to just replace the existing braces exactly as they are.
        if (wasOnSingleLine)
        {
            // convert '{ ' to '['
            if (openBracket.TrailingTrivia is [(kind: SyntaxKind.WhitespaceTrivia), ..])
                openBracket = openBracket.WithTrailingTrivia(openBracket.TrailingTrivia.Skip(1));

            if (elements is [.., var lastNodeOrToken] && lastNodeOrToken.GetTrailingTrivia() is [.., (kind: SyntaxKind.WhitespaceTrivia)] trailingTrivia)
                elements = elements.Replace(lastNodeOrToken, lastNodeOrToken.WithTrailingTrivia(trailingTrivia.Take(trailingTrivia.Count - 1)));
        }

        return CollectionExpression(openBracket, SeparatedList<CollectionElementSyntax>(elements), closeBracket);
    }

    public static CollectionExpressionSyntax ReplaceWithCollectionExpression(
        SourceText sourceText,
        InitializerExpressionSyntax originalInitializer,
        CollectionExpressionSyntax newCollectionExpression,
        bool newCollectionIsSingleLine)
    {
        Contract.ThrowIfFalse(originalInitializer.Parent
            is ArrayCreationExpressionSyntax
            or ImplicitArrayCreationExpressionSyntax
            or StackAllocArrayCreationExpressionSyntax
            or ImplicitStackAllocArrayCreationExpressionSyntax
            or BaseObjectCreationExpressionSyntax);

        var initializerParent = originalInitializer.GetRequiredParent();

        return ShouldReplaceExistingExpressionEntirely(sourceText, originalInitializer, newCollectionIsSingleLine)
            ? newCollectionExpression.WithTriviaFrom(initializerParent)
            : newCollectionExpression
                .WithPrependedLeadingTrivia(originalInitializer.OpenBraceToken.GetPreviousToken().TrailingTrivia)
                .WithPrependedLeadingTrivia(ElasticMarker);
    }

    private static bool ShouldReplaceExistingExpressionEntirely(
        SourceText sourceText,
        InitializerExpressionSyntax initializer,
        bool newCollectionIsSingleLine)
    {
        if (initializer.OpenBraceToken.GetPreviousToken().TrailingTrivia.Any(static x => x.IsSingleOrMultiLineComment()))
            return false;

        // Any time we have `{ x, y, z }` in any form, then always just replace the whole original expression
        // with `[x, y, z]`.
        if (newCollectionIsSingleLine && sourceText.AreOnSameLine(initializer.OpenBraceToken, initializer.CloseBraceToken))
            return true;

        // initializer was on multiple lines, but started on the same line as the 'new' keyword.  e.g.:
        //
        //      var v = new[] {
        //          1, 2, 3
        //      };
        //
        // Just remove the `new...` section entirely, but otherwise keep the initialize multiline:
        //
        //      var v = [
        //          1, 2, 3
        //      ];
        var parent = initializer.GetRequiredParent();
        var newKeyword = parent.GetFirstToken();
        if (sourceText.AreOnSameLine(newKeyword, initializer.OpenBraceToken) &&
            !sourceText.AreOnSameLine(initializer.OpenBraceToken, initializer.CloseBraceToken))
        {
            return true;
        }

        // Initializer was on multiple lines, and was not on the same line as the 'new' keyword, and the 'new' is on a newline:
        //
        //      var v2 =
        //          new[]
        //          {
        //              1, 2, 3
        //          };
        //
        // For this latter, we want to just remove the new portion and move the collection to subsume it.
        var previousToken = newKeyword.GetPreviousToken();
        if (previousToken == default)
            return true;

        if (!sourceText.AreOnSameLine(previousToken, newKeyword))
            return true;

        // All that is left is:
        //
        //      var v2 = new[]
        //      {
        //          1, 2, 3
        //      };
        //
        // For this we want to remove the 'new' portion, but keep the collection on its own line.
        return false;
    }

    public static ImmutableArray<CollectionMatch<StatementSyntax>> TryGetMatches<TArrayCreationExpressionSyntax>(
        SemanticModel semanticModel,
        TArrayCreationExpressionSyntax expression,
        CollectionExpressionSyntax replacementExpression,
        INamedTypeSymbol? expressionType,
        bool isSingletonInstance,
        bool allowSemanticsChange,
        Func<TArrayCreationExpressionSyntax, TypeSyntax> getType,
        Func<TArrayCreationExpressionSyntax, InitializerExpressionSyntax?> getInitializer,
        CancellationToken cancellationToken,
        out bool changesSemantics)
        where TArrayCreationExpressionSyntax : ExpressionSyntax
    {
        Contract.ThrowIfFalse(expression is ArrayCreationExpressionSyntax or StackAllocArrayCreationExpressionSyntax);

        changesSemantics = false;

        // has to either be `stackalloc X[]` or `stackalloc X[const]`.
        if (getType(expression) is not ArrayTypeSyntax { RankSpecifiers: [{ Sizes: [var size] }, ..] })
            return default;

        using var _ = ArrayBuilder<CollectionMatch<StatementSyntax>>.GetInstance(out var matches);

        var initializer = getInitializer(expression);
        if (size is OmittedArraySizeExpressionSyntax)
        {
            // `stackalloc int[]` on its own is illegal.  Has to either have a size, or an initializer.
            if (initializer is null)
                return default;
        }
        else
        {
            // if `stackalloc X[val]`, then it `val` has to be a constant value.
            if (semanticModel.GetConstantValue(size, cancellationToken).Value is not int sizeValue)
                return default;

            if (initializer != null)
            {
                // if there is an initializer, then it has to have the right number of elements.
                if (sizeValue != initializer.Expressions.Count)
                    return default;
            }
            else
            {
                // if there is no initializer, we have to be followed by direct statements that initialize the right
                // number of elements.  But if you just have `new int[0]` that can always be replaced.
                if (sizeValue > 0)
                {
                    // This needs to be local variable like `ReadOnlySpan<T> x = stackalloc ...
                    if (expression.WalkUpParentheses().Parent is not EqualsValueClauseSyntax
                        {
                            Parent: VariableDeclaratorSyntax
                            {
                                Identifier.ValueText: var variableName,
                                Parent.Parent: LocalDeclarationStatementSyntax localDeclarationStatement
                            } variableDeclarator,
                        })
                    {
                        return default;
                    }

                    var localSymbol = semanticModel.GetRequiredDeclaredSymbol(variableDeclarator, cancellationToken);

                    var currentStatement = localDeclarationStatement.GetNextStatement();
                    for (var currentIndex = 0; currentIndex < sizeValue; currentIndex++)
                    {
                        // Each following statement needs to of the form:
                        //
                        //   x[...] =
                        if (currentStatement is not ExpressionStatementSyntax
                            {
                                Expression: AssignmentExpressionSyntax
                                {
                                    Left: ElementAccessExpressionSyntax
                                    {
                                        Expression: IdentifierNameSyntax { Identifier.ValueText: var elementName },
                                        ArgumentList.Arguments: [var elementArgument],
                                    } elementAccess,
                                } assignmentExpression,
                            } expressionStatement)
                        {
                            return default;
                        }

                        // Ensure we're indexing into the variable created.
                        if (variableName != elementName)
                            return default;

                        // The indexing value has to equal the corresponding location in the result.
                        if (semanticModel.GetConstantValue(elementArgument.Expression, cancellationToken).Value is not int indexValue ||
                            indexValue != currentIndex)
                        {
                            return default;
                        }

                        // If we have an array whose elements points back to the array itself, then we can't convert
                        // this to a collection expression.
                        if (assignmentExpression.Right.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>().Any(
                                i => localSymbol.Equals(semanticModel.GetSymbolInfo(i, cancellationToken).GetAnySymbol())))
                        {
                            return default;
                        }

                        // this looks like a good statement, add to the right size of the assignment to track as that's what
                        // we'll want to put in the final collection expression.
                        matches.Add(new(expressionStatement, UseSpread: false));
                        currentStatement = currentStatement.GetNextStatement();
                    }
                }
            }
        }

        if (!CanReplaceWithCollectionExpression(
                semanticModel, expression, replacementExpression, expressionType, isSingletonInstance, allowSemanticsChange, skipVerificationForReplacedNode: true, cancellationToken, out changesSemantics))
        {
            return default;
        }

        return matches.ToImmutableAndClear();
    }

    public static bool IsCollectionFactoryCreate(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocationExpression,
        [NotNullWhen(true)] out MemberAccessExpressionSyntax? memberAccess,
        out bool unwrapArgument,
        out bool useSpread,
        CancellationToken cancellationToken)
    {
        const string CreateName = nameof(ImmutableArray.Create);
        const string CreateRangeName = nameof(ImmutableArray.CreateRange);

        unwrapArgument = false;
        useSpread = false;
        memberAccess = null;

        // Looking for `XXX.Create(...)`
        if (invocationExpression.Expression is not MemberAccessExpressionSyntax
            {
                RawKind: (int)SyntaxKind.SimpleMemberAccessExpression,
                Name.Identifier.Value: CreateName or CreateRangeName,
            } memberAccessExpression)
        {
            return false;
        }

        memberAccess = memberAccessExpression;
        if (semanticModel.GetSymbolInfo(memberAccessExpression, cancellationToken).Symbol is not IMethodSymbol { IsStatic: true } createMethod)
            return false;

        if (semanticModel.GetSymbolInfo(memberAccessExpression.Expression, cancellationToken).Symbol is not INamedTypeSymbol factoryType)
            return false;

        var compilation = semanticModel.Compilation;

        // The pattern is a type like `ImmutableArray` (non-generic), returning an instance of `ImmutableArray<T>`.  The
        // actual collection type (`ImmutableArray<T>`) has to have a `[CollectionBuilder(...)]` attribute on it that
        // then points at the factory type.
        var collectionBuilderAttributeData = createMethod.ReturnType.OriginalDefinition
            .GetAttributes()
            .FirstOrDefault(a => a.AttributeClass.IsCollectionBuilderAttribute());
        if (collectionBuilderAttributeData?.ConstructorArguments is not [{ Value: ITypeSymbol collectionBuilderType }, { Value: CreateName }])
            return false;

        if (!factoryType.OriginalDefinition.Equals(collectionBuilderType.OriginalDefinition))
            return false;

        // Ok, this is type that has a collection-builder option available.  We can switch over if the current method
        // we're calling has one of the following signatures:
        //
        //  `Create()`.  Trivial case, can be replaced with `[]`.
        //  `Create(T), Create(T, T), Create(T, T, T)` etc.
        //  `Create(params T[])` (passing as individual elements, or an array with an initializer)
        //  `Create(ReadOnlySpan<T>)` (passing as a stack-alloc with an initializer)
        //  `Create(IEnumerable<T>)` (passing as something with an initializer.
        if (!IsCompatibleSignatureAndArguments(createMethod.OriginalDefinition, out unwrapArgument, out useSpread))
            return false;

        return true;

        bool IsCompatibleSignatureAndArguments(
            IMethodSymbol originalCreateMethod,
            out bool unwrapArgument,
            out bool useSpread)
        {
            unwrapArgument = false;
            useSpread = false;

            var arguments = invocationExpression.ArgumentList.Arguments;

            // Don't bother offering if any of the arguments are named.  It's unlikely for this to occur in practice, and it
            // means we do not have to worry about order of operations.
            if (arguments.Any(static a => a.NameColon != null))
                return false;

            if (originalCreateMethod.Name is CreateRangeName)
            {
                // If we have `CreateRange<T>(IEnumerable<T> values)` this is legal if we have an array, or no-arg object creation.
                if (originalCreateMethod.Parameters is [
                    {
                        Type: INamedTypeSymbol
                        {
                            Name: nameof(IEnumerable<>),
                            TypeArguments: [ITypeParameterSymbol { TypeParameterKind: TypeParameterKind.Method }]
                        } enumerableType
                    }] &&
                    enumerableType.OriginalDefinition.Equals(compilation.IEnumerableOfTType()) &&
                    arguments.Count == 1)
                {
                    return IsArgumentCompatibleWithIEnumerableOfT(semanticModel, arguments[0], out unwrapArgument, out useSpread, cancellationToken);
                }
            }
            else if (originalCreateMethod.Name is CreateName)
            {
                // `XXX.Create()` can be converted to `[]`
                if (originalCreateMethod.Parameters.Length == 0)
                    return arguments.Count == 0;

                // If we have `Create<T>(T)`, `Create<T>(T, T)` etc., then this is convertible.
                if (originalCreateMethod.Parameters.All(static p => p.Type is ITypeParameterSymbol { TypeParameterKind: TypeParameterKind.Method }))
                    return arguments.Count == originalCreateMethod.Parameters.Length;

                // If we have `Create<T>(params T[])` this is legal if there are multiple arguments.  Or a single argument that
                // is an array literal.
                if (originalCreateMethod.Parameters is [{ IsParams: true, Type: IArrayTypeSymbol { ElementType: ITypeParameterSymbol { TypeParameterKind: TypeParameterKind.Method } } }])
                {
                    if (arguments.Count >= 2)
                        return true;

                    if (arguments is [{ Expression: ArrayCreationExpressionSyntax { Initializer: not null } or ImplicitArrayCreationExpressionSyntax }])
                    {
                        unwrapArgument = true;
                        return true;
                    }

                    return false;
                }

                // If we have `Create<T>(ReadOnlySpan<T> values)` this is legal if a stack-alloc expression is passed along.
                //
                // Runtime needs to support inline arrays in order for this to be ok.  Otherwise compiler will change the
                // stack alloc to a heap alloc, which could be very bad for user perf.

                if (arguments.Count == 1 &&
                    compilation.SupportsRuntimeCapability(RuntimeCapability.InlineArrayTypes) &&
                    originalCreateMethod.Parameters is [
                        {
                            Type: INamedTypeSymbol
                            {
                                Name: nameof(Span<>) or nameof(ReadOnlySpan<>),
                                TypeArguments: [ITypeParameterSymbol { TypeParameterKind: TypeParameterKind.Method }]
                            } spanType
                        }])
                {
                    if (spanType.OriginalDefinition.Equals(compilation.SpanOfTType()) ||
                        spanType.OriginalDefinition.Equals(compilation.ReadOnlySpanOfTType()))
                    {
                        if (arguments[0].Expression
                                is StackAllocArrayCreationExpressionSyntax { Initializer: not null }
                                or ImplicitStackAllocArrayCreationExpressionSyntax)
                        {
                            unwrapArgument = true;
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }

    public static bool IsArgumentCompatibleWithIEnumerableOfT(
        SemanticModel semanticModel, ArgumentSyntax argument, out bool unwrapArgument, out bool useSpread, CancellationToken cancellationToken)
    {
        unwrapArgument = false;
        useSpread = false;

        var argExpression = argument.Expression;
        if (argExpression
                is ArrayCreationExpressionSyntax { Initializer: not null }
                or ImplicitArrayCreationExpressionSyntax)
        {
            unwrapArgument = true;
            return true;
        }

        if (argExpression is ObjectCreationExpressionSyntax objectCreation)
        {
            // Can't have any arguments, as we cannot preserve them once we grab out all the elements.
            if (objectCreation.ArgumentList is { Arguments.Count: > 0 })
                return false;

            // If it's got an initializer, it has to be a collection initializer (or an empty object initializer);
            if (objectCreation.Initializer.IsKind(SyntaxKind.ObjectCreationExpression) && objectCreation.Initializer.Expressions.Count > 0)
                return false;

            unwrapArgument = true;
            return true;
        }

        if (IsIterable(semanticModel, argExpression, cancellationToken))
        {
            // Convert `ImmutableArray.Create(someEnumerable)` to `[.. someEnumerable]`
            unwrapArgument = false;
            useSpread = true;
            return true;
        }

        return false;
    }

    public static bool IsIterable(SemanticModel semanticModel, ExpressionSyntax expression, CancellationToken cancellationToken)
    {
        var type = semanticModel.GetTypeInfo(expression, cancellationToken).Type;
        if (type is null or IErrorTypeSymbol)
            return false;

        if (BannedTypes.Contains(type.Name))
            return false;

        var compilation = semanticModel.Compilation;
        return EqualsOrImplements(type, compilation.IEnumerableOfTType()) ||
            type.Equals(compilation.SpanOfTType()) ||
            type.Equals(compilation.ReadOnlySpanOfTType());
    }

    public static bool EqualsOrImplements(ITypeSymbol type, INamedTypeSymbol? interfaceType)
    {
        if (interfaceType != null)
        {
            if (type.OriginalDefinition.Equals(interfaceType))
                return true;

            foreach (var baseInterface in type.AllInterfaces)
            {
                if (interfaceType.Equals(baseInterface.OriginalDefinition))
                    return true;
            }
        }

        return false;
    }

    public static bool IsCollectionEmptyAccess(
        SemanticModel semanticModel,
        ExpressionSyntax expression,
        CancellationToken cancellationToken)
    {
        const string EmptyName = nameof(Array.Empty);

        if (expression is MemberAccessExpressionSyntax memberAccess)
        {
            // X<T>.Empty
            return IsEmptyProperty(memberAccess);
        }
        else if (expression is InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax innerMemberAccess } invocation)
        {
            // X.Empty<T>()
            return IsEmptyMethodCall(invocation, innerMemberAccess);
        }
        else
        {
            return false;
        }

        // X<T>.Empty
        bool IsEmptyProperty(MemberAccessExpressionSyntax memberAccess)
        {
            if (!IsPossiblyDottedGenericName(memberAccess.Expression))
                return false;

            if (memberAccess.Name is not IdentifierNameSyntax { Identifier.ValueText: EmptyName })
                return false;

            var expressionSymbol = semanticModel.GetSymbolInfo(memberAccess.Expression, cancellationToken).Symbol;
            if (expressionSymbol is not INamedTypeSymbol)
                return false;

            var emptySymbol = semanticModel.GetSymbolInfo(memberAccess, cancellationToken).Symbol;
            if (emptySymbol is not { IsStatic: true })
                return false;

            if (emptySymbol is not IFieldSymbol and not IPropertySymbol)
                return false;

            return true;
        }

        // X.Empty<T>()
        bool IsEmptyMethodCall(InvocationExpressionSyntax invocation, MemberAccessExpressionSyntax memberAccess)
        {
            if (invocation.ArgumentList.Arguments.Count != 0)
                return false;

            if (memberAccess.Name is not GenericNameSyntax
                {
                    TypeArgumentList.Arguments.Count: 1,
                    Identifier.ValueText: EmptyName,
                })
            {
                return false;
            }

            if (!IsPossiblyDottedName(memberAccess.Expression))
                return false;

            var expressionSymbol = semanticModel.GetSymbolInfo(memberAccess.Expression, cancellationToken).Symbol;
            if (expressionSymbol is not INamedTypeSymbol)
                return false;

            var emptySymbol = semanticModel.GetSymbolInfo(memberAccess, cancellationToken).Symbol;
            if (emptySymbol is not { IsStatic: true })
                return false;

            if (emptySymbol is not IMethodSymbol)
                return false;

            return true;
        }

        static bool IsPossiblyDottedGenericName(ExpressionSyntax expression)
        {
            if (expression is GenericNameSyntax)
                return true;

            if (expression is MemberAccessExpressionSyntax { Expression: ExpressionSyntax childName, Name: GenericNameSyntax } &&
                IsPossiblyDottedName(childName))
            {
                return true;
            }

            return false;
        }

        static bool IsPossiblyDottedName(ExpressionSyntax name)
        {
            if (name is IdentifierNameSyntax)
                return true;

            if (name is MemberAccessExpressionSyntax { Expression: ExpressionSyntax childName, Name: IdentifierNameSyntax } &&
                IsPossiblyDottedName(childName))
            {
                return true;
            }

            return false;
        }
    }

    public static SeparatedSyntaxList<ArgumentSyntax> GetArguments(ArgumentListSyntax argumentList, bool unwrapArgument)
    {
        var arguments = argumentList.Arguments;

        // If we're not unwrapping a singular argument expression, then just pass back all the explicit argument
        // expressions the user wrote out.
        if (!unwrapArgument)
            return arguments;

        Contract.ThrowIfTrue(arguments.Count != 1);
        var expression = arguments.Single().Expression;

        var initializer = expression switch
        {
            ImplicitArrayCreationExpressionSyntax implicitArray => implicitArray.Initializer,
            ImplicitStackAllocArrayCreationExpressionSyntax implicitStackAlloc => implicitStackAlloc.Initializer,
            ArrayCreationExpressionSyntax arrayCreation => arrayCreation.Initializer,
            StackAllocArrayCreationExpressionSyntax stackAllocCreation => stackAllocCreation.Initializer,
            ImplicitObjectCreationExpressionSyntax implicitObjectCreation => implicitObjectCreation.Initializer,
            ObjectCreationExpressionSyntax objectCreation => objectCreation.Initializer,
            _ => throw ExceptionUtilities.Unreachable(),
        };

        return initializer is null
            ? default
            : SeparatedList<ArgumentSyntax>(initializer.Expressions.GetWithSeparators().Select(
                nodeOrToken => nodeOrToken.IsToken ? nodeOrToken : Argument((ExpressionSyntax)nodeOrToken.AsNode()!)));
    }

    public static CollectionExpressionSyntax CreateReplacementCollectionExpressionForAnalysis(InitializerExpressionSyntax? initializer)
        => initializer is null ? s_emptyCollectionExpression : CollectionExpression([.. initializer.Expressions.Select(ExpressionElement)]);

    public static ImmutableDictionary<string, string?> GetDiagnosticProperties(bool unwrapArgument, bool useSpread, bool changesSemantics)
    {
        var properties = ImmutableDictionary<string, string?>.Empty;

        if (unwrapArgument)
            properties = properties.Add(UnwrapArgument, "");

        if (useSpread)
            properties = properties.Add(UseSpread, "");

        if (changesSemantics)
            properties = properties.Add(UseCollectionInitializerHelpers.ChangesSemanticsName, "");

        return properties;
    }
}
