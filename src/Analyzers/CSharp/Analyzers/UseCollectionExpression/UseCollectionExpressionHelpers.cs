﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp.Utilities;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.UseCollectionExpression;

using static SyntaxFactory;

internal static class UseCollectionExpressionHelpers
{
    private static readonly CollectionExpressionSyntax s_emptyCollectionExpression = CollectionExpression();

    public static bool CanReplaceWithCollectionExpression(
        SemanticModel semanticModel,
        ExpressionSyntax expression,
        bool skipVerificationForReplacedNode,
        CancellationToken cancellationToken)
    {
        var compilation = semanticModel.Compilation;

        var topMostExpression = expression.WalkUpParentheses();
        if (topMostExpression.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error))
            return false;

        var parent = topMostExpression.GetRequiredParent();

        if (!IsInTargetTypedLocation(semanticModel, topMostExpression, cancellationToken))
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

        if (!IsConstructibleCollectionType(originalTypeInfo.ConvertedType.OriginalDefinition))
            return false;

        // Conservatively, avoid making this change if the original expression was itself converted. Consider, for
        // example, `IEnumerable<string> x = new List<string>()`.  If we change that to `[]` we will still compile,
        // but it's possible we'll end up with different types at runtime that may cause problems.
        //
        // Note: we can relax this on a case by case basis if we feel like it's acceptable.
        if (originalTypeInfo.Type != null &&
            !originalTypeInfo.Type.Equals(originalTypeInfo.ConvertedType) &&
            !IsSafeConversionWhenTypesDoNotMatch())
        {
            return false;
        }

        // HACK: Workaround lack of compiler information for collection expression conversions with casts.
        // Specifically, hardcode in knowledge that a cast to a constructible collection type of the empty collection
        // expression will always succeed, and there's no need to actually validate semantics there.
        // Tracked by https://github.com/dotnet/roslyn/issues/68826
        if (parent is CastExpressionSyntax)
            return IsConstructibleCollectionType(semanticModel.GetTypeInfo(parent, cancellationToken).Type);

        // Looks good as something to replace.  Now check the semantics of making the replacement to see if there would
        // any issues.  To keep things simple, all we do is replace the existing expression with the `[]` literal. This
        // is an 'untyped' collection expression literal, so it tells us if the new code will have any issues moving to
        // something untyped.  This will also tell us if we have any ambiguities (because there are multiple destination
        // types that could accept the collection expression).
        var speculationAnalyzer = new SpeculationAnalyzer(
            topMostExpression,
            s_emptyCollectionExpression,
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
        var replacedTypeInfo = speculationAnalyzer.SpeculativeSemanticModel.GetTypeInfo(speculationAnalyzer.ReplacedExpression, cancellationToken);
        if (!originalTypeInfo.ConvertedType.Equals(replacedTypeInfo.ConvertedType))
            return false;

        return true;

        bool IsConstructibleCollectionType(ITypeSymbol? type)
        {
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
        }

        IMethodSymbol? GetAccessibleInstanceConstructor(ImmutableArray<IMethodSymbol> constructors, Func<IMethodSymbol, bool> predicate)
        {
            var constructor = constructors.FirstOrDefault(c => !c.IsStatic && predicate(c));
            return constructor is not null && constructor.IsAccessibleWithin(compilation.Assembly) ? constructor : null;
        }

        bool IsSafeConversionWhenTypesDoNotMatch()
        {
            var type = originalTypeInfo.Type;
            var convertedType = originalTypeInfo.ConvertedType;

            var convertedToReadOnlySpan =
                convertedType.Name == nameof(ReadOnlySpan<int>) &&
                convertedType.OriginalDefinition.Equals(compilation.ReadOnlySpanOfTType());

            var convertedToSpan =
                convertedType.Name == nameof(Span<int>) &&
                convertedType.OriginalDefinition.Equals(compilation.SpanOfTType());

            // ReadOnlySpan<X> x = stackalloc[] ...
            //
            // This will be a Span<X> converted to a ReadOnlySpan<X>.  This is always safe as ReadOnlySpan is more
            // restrictive than Span<X>
            var isSpanToReadOnlySpan =
                convertedToReadOnlySpan &&
                type.Name == nameof(Span<int>) &&
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

            // Add more cases to support here.
            return false;
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

    private static bool IsInTargetTypedLocation(SemanticModel semanticModel, ExpressionSyntax expression, CancellationToken cancellationToken)
    {
        var topExpression = expression.WalkUpParentheses();
        var parent = topExpression.Parent;
        return parent switch
        {
            EqualsValueClauseSyntax equalsValue => IsInTargetTypedEqualsValueClause(equalsValue),
            CastExpressionSyntax castExpression => IsInTargetTypedCastExpression(castExpression),
            // a ? [1, 2, 3] : ...  is target typed if either the other side is *not* a collection,
            // or the entire ternary is target typed itself.
            ConditionalExpressionSyntax conditionalExpression => IsInTargetTypedConditionalExpression(conditionalExpression, topExpression),
            // Similar rules for switches.
            SwitchExpressionArmSyntax switchExpressionArm => IsInTargetTypedSwitchExpressionArm(switchExpressionArm),
            InitializerExpressionSyntax initializerExpression => IsInTargetTypedInitializerExpression(initializerExpression, topExpression),
            CollectionElementSyntax collectionElement => IsInTargetTypedCollectionElement(collectionElement),
            AssignmentExpressionSyntax assignmentExpression => IsInTargetTypedAssignmentExpression(assignmentExpression, topExpression),
            BinaryExpressionSyntax binaryExpression => IsInTargetTypedBinaryExpression(binaryExpression, topExpression),
            ArgumentSyntax or AttributeArgumentSyntax => true,
            ReturnStatementSyntax => true,
            ArrowExpressionClauseSyntax => true,
            _ => false,
        };

        bool HasType(ExpressionSyntax expression)
            => semanticModel.GetTypeInfo(expression, cancellationToken).Type is not null and not IErrorTypeSymbol;

        static bool IsInTargetTypedEqualsValueClause(EqualsValueClauseSyntax equalsValue)
            // If we're after an `x = ...` and it's not `var x`, this is target typed.
            => equalsValue.Parent is not VariableDeclaratorSyntax { Parent: VariableDeclarationSyntax { Type.IsVar: true } };

        static bool IsInTargetTypedCastExpression(CastExpressionSyntax castExpression)
            // (X[])[1, 2, 3] is target typed.  `(X)[1, 2, 3]` is currently not (because it looks like indexing into an expr).
            => castExpression.Type is not IdentifierNameSyntax;

        bool IsInTargetTypedConditionalExpression(ConditionalExpressionSyntax conditionalExpression, ExpressionSyntax expression)
        {
            if (conditionalExpression.WhenTrue == expression)
                return HasType(conditionalExpression.WhenFalse) || IsInTargetTypedLocation(semanticModel, conditionalExpression, cancellationToken);
            else if (conditionalExpression.WhenFalse == expression)
                return HasType(conditionalExpression.WhenTrue) || IsInTargetTypedLocation(semanticModel, conditionalExpression, cancellationToken);
            else
                return false;
        }

        bool IsInTargetTypedSwitchExpressionArm(SwitchExpressionArmSyntax switchExpressionArm)
        {
            var switchExpression = (SwitchExpressionSyntax)switchExpressionArm.GetRequiredParent();

            // check if any other arm has a type that this would be target typed against.
            foreach (var arm in switchExpression.Arms)
            {
                if (arm != switchExpressionArm && HasType(arm.Expression))
                    return true;
            }

            // All arms do not have a type, this is target typed if the switch itself is target typed.
            return IsInTargetTypedLocation(semanticModel, switchExpression, cancellationToken);
        }

        bool IsInTargetTypedCollectionElement(CollectionElementSyntax collectionElement)
        {
            // We do not currently target type spread expressions in a collection expression.
            if (collectionElement is not ExpressionElementSyntax)
                return false;

            // The element it target typed if the parent collection is itself target typed.
            var collectionExpression = (CollectionExpressionSyntax)collectionElement.GetRequiredParent();
            return IsInTargetTypedLocation(semanticModel, collectionExpression, cancellationToken);
        }

        bool IsInTargetTypedInitializerExpression(InitializerExpressionSyntax initializerExpression, ExpressionSyntax expression)
        {
            // new X[] { [1, 2, 3] }.  Elements are target typed by array type.
            if (initializerExpression.Parent is ArrayCreationExpressionSyntax)
                return true;

            // new [] { [1, 2, 3], ... }.  Elements are target typed if there's another element with real type.
            if (initializerExpression.Parent is ImplicitArrayCreationExpressionSyntax)
            {
                foreach (var sibling in initializerExpression.Expressions)
                {
                    if (sibling != expression && HasType(sibling))
                        return true;
                }
            }

            // TODO: Handle these.
            if (initializerExpression.Parent is StackAllocArrayCreationExpressionSyntax or ImplicitStackAllocArrayCreationExpressionSyntax)
                return false;

            // T[] x = [1, 2, 3];
            if (initializerExpression.Parent is EqualsValueClauseSyntax)
                return true;

            return false;
        }

        bool IsInTargetTypedAssignmentExpression(AssignmentExpressionSyntax assignmentExpression, ExpressionSyntax expression)
        {
            return expression == assignmentExpression.Right && HasType(assignmentExpression.Left);
        }

        bool IsInTargetTypedBinaryExpression(BinaryExpressionSyntax binaryExpression, ExpressionSyntax expression)
        {
            return binaryExpression.Kind() == SyntaxKind.CoalesceExpression && binaryExpression.Right == expression && HasType(binaryExpression.Left);
        }
    }

    public static CollectionExpressionSyntax ConvertInitializerToCollectionExpression(
        InitializerExpressionSyntax initializer, bool wasOnSingleLine)
    {
        // if the initializer is already on multiple lines, keep it that way.  otherwise, squash from `{ 1, 2, 3 }` to `[1, 2, 3]`
        var openBracket = Token(SyntaxKind.OpenBracketToken).WithTriviaFrom(initializer.OpenBraceToken);
        var elements = initializer.Expressions.GetWithSeparators().SelectAsArray(
            i => i.IsToken ? i : ExpressionElement((ExpressionSyntax)i.AsNode()!));
        var closeBracket = Token(SyntaxKind.CloseBracketToken).WithTriviaFrom(initializer.CloseBraceToken);

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

    public static ImmutableArray<CollectionExpressionMatch<StatementSyntax>> TryGetMatches<TArrayCreationExpressionSyntax>(
        SemanticModel semanticModel,
        TArrayCreationExpressionSyntax expression,
        Func<TArrayCreationExpressionSyntax, TypeSyntax> getType,
        Func<TArrayCreationExpressionSyntax, InitializerExpressionSyntax?> getInitializer,
        CancellationToken cancellationToken)
        where TArrayCreationExpressionSyntax : ExpressionSyntax
    {
        Contract.ThrowIfFalse(expression is ArrayCreationExpressionSyntax or StackAllocArrayCreationExpressionSyntax);

        // has to either be `stackalloc X[]` or `stackalloc X[const]`.
        if (getType(expression) is not ArrayTypeSyntax { RankSpecifiers: [{ Sizes: [var size] }, ..] })
            return default;

        using var _ = ArrayBuilder<CollectionExpressionMatch<StatementSyntax>>.GetInstance(out var matches);

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
                semanticModel, expression, skipVerificationForReplacedNode: true, cancellationToken))
        {
            return default;
        }

        return matches.ToImmutable();
    }

    public static bool IsCollectionFactoryCreate(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocationExpression,
        [NotNullWhen(true)] out MemberAccessExpressionSyntax? memberAccess,
        out bool unwrapArgument,
        CancellationToken cancellationToken)
    {
        const string CreateName = nameof(ImmutableArray.Create);
        const string CreateRangeName = nameof(ImmutableArray.CreateRange);

        unwrapArgument = false;
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
        if (!IsCompatibleSignatureAndArguments(createMethod.OriginalDefinition, out unwrapArgument))
            return false;

        return true;

        bool IsCompatibleSignatureAndArguments(
            IMethodSymbol originalCreateMethod,
            out bool unwrapArgument)
        {
            unwrapArgument = false;

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
                            Name: nameof(IEnumerable<int>),
                            TypeArguments: [ITypeParameterSymbol { TypeParameterKind: TypeParameterKind.Method }]
                        } enumerableType
                    }] && enumerableType.OriginalDefinition.Equals(compilation.IEnumerableOfTType()))
                {
                    var argExpression = arguments[0].Expression;
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
                        if (objectCreation.ArgumentList != null && objectCreation.ArgumentList.Arguments.Count > 0)
                            return false;

                        // If it's got an initializer, it has to be a collection initializer (or an empty object initializer);
                        if (objectCreation.Initializer.IsKind(SyntaxKind.ObjectCreationExpression) && objectCreation.Initializer.Expressions.Count > 0)
                            return false;

                        unwrapArgument = true;
                        return true;
                    }
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
                            Name: nameof(Span<int>) or nameof(ReadOnlySpan<int>),
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

    public static SeparatedSyntaxList<ArgumentSyntax> GetArguments(InvocationExpressionSyntax invocationExpression, bool unwrapArgument)
    {
        var arguments = invocationExpression.ArgumentList.Arguments;

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
}
