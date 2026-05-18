// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        // -----------------------------------------------------------------------
        // CSX (JSX-like component syntax) lowering — classic runtime
        //
        // <Button Color="red">
        //     <Icon Name="star" />
        //     Some text
        //     {items}     ← IEnumerable<IElement?> spread
        // </Button>
        //
        // Non-spread case lowers to:
        //
        // H.CreateElement(
        //     Button.Render,
        //     new ButtonProps(Color: "red"),
        //     new IElement?[] { child1, child2, ... })
        //
        // Spread case lowers to:
        //
        // H.CreateElement(
        //     Button.Render,
        //     new ButtonProps(Color: "red"),
        //     __BuildChildren())
        //
        // where __BuildChildren is inlined as:
        //
        //   var __list = new List<IElement?>();
        //   __list.Add(child1);
        //   foreach (var __item in spreadExpr) __list.Add(__item);
        //   __list.Add(child2);
        //   result = __list.ToArray()
        // -----------------------------------------------------------------------

        public override BoundNode VisitCsxElement(BoundCsxElement node)
        {
            // ---- 1. Resolve the concrete component method ----
            var componentMethod = node.ComponentMethod;

            // ---- 2. Determine props type (first parameter of the component method) ----
            var propsType = componentMethod.Parameters.Length > 0
                ? componentMethod.Parameters[0].Type
                : null;

            // ---- 3. Construct CreateElement<TProps> ----
            var factoryMethod = node.FactoryMethod;
            if (factoryMethod.IsGenericMethod && propsType is not null)
            {
                factoryMethod = factoryMethod.Construct(ImmutableArray.Create(propsType));
            }

            // ---- 4. Build the component delegate arg ----
            BoundExpression componentArg;
            if (factoryMethod.Parameters.Length > 0)
            {
                var delegateType = factoryMethod.Parameters[0].Type;
                var containingType = componentMethod.ContainingType;
                var typeExpr = new BoundTypeExpression(
                    syntax: node.Syntax,
                    aliasOpt: null,
                    type: containingType)
                { WasCompilerGenerated = true };
                componentArg = new BoundDelegateCreationExpression(
                    syntax: node.Syntax,
                    argument: typeExpr,
                    methodOpt: componentMethod,
                    isExtensionMethod: false,
                    wasTargetTyped: false,
                    type: delegateType)
                { WasCompilerGenerated = true };
            }
            else
            {
                componentArg = VisitExpression(node.ComponentArgument);
            }

            // ---- 5. Lower the props argument ----
            BoundExpression? propsArg = node.PropsArgument is not null
                ? VisitExpression(node.PropsArgument)
                : null;

            // ---- 6. Lower children ----
            var elementType = node.Type; // CSX.IElement
            var loweredChildren = VisitList(node.Children);
            var childrenArg = BuildChildrenArg(node, elementType, loweredChildren);

            // ---- 7. Assemble args ----
            ImmutableArray<BoundExpression> args;
            if (propsArg is not null)
                args = ImmutableArray.Create(componentArg, propsArg, childrenArg);
            else
                args = ImmutableArray.Create(componentArg, childrenArg);

            return _factory.StaticCall(factoryMethod, args);
        }

        /// <summary>
        /// Builds the children array argument for <c>CreateElement</c>.
        /// <list type="bullet">
        ///   <item>No children → <c>Array.Empty&lt;T&gt;()</c></item>
        ///   <item>No spreads → <c>new T[] { c0, c1, … }</c></item>
        ///   <item>Has spreads → <c>List&lt;T&gt;</c> with <c>Add</c>/<c>AddRange</c>, then <c>ToArray()</c></item>
        /// </list>
        /// </summary>
        private BoundExpression BuildChildrenArg(
            BoundCsxElement node,
            TypeSymbol elementType,
            ImmutableArray<BoundExpression> loweredChildren)
        {
            if (loweredChildren.IsEmpty)
                return _factory.ArrayOrEmpty(elementType, loweredChildren);

            // Check whether any child is a spread (IEnumerable<elementType>).
            bool hasSpread = false;
            foreach (var child in loweredChildren)
            {
                if (IsSpreadChild(child, elementType))
                {
                    hasSpread = true;
                    break;
                }
            }

            if (!hasSpread)
                return _factory.ArrayOrEmpty(elementType, loweredChildren);

            // ---- Spread path: build via List<elementType> ----
            var listOfT = _factory.WellKnownType(WellKnownType.System_Collections_Generic_List_T)
                .Construct(ImmutableArray.Create(elementType));

            var listCtor = ((MethodSymbol)_factory.WellKnownMember(WellKnownMember.System_Collections_Generic_List_T__ctor))
                .AsMember(listOfT);
            var listAdd = ((MethodSymbol)_factory.WellKnownMember(WellKnownMember.System_Collections_Generic_List_T__Add))
                .AsMember(listOfT);
            var listAddRange = ((MethodSymbol)_factory.WellKnownMember(WellKnownMember.System_Collections_Generic_List_T__AddRange))
                .AsMember(listOfT);
            var listToArray = ((MethodSymbol)_factory.WellKnownMember(WellKnownMember.System_Collections_Generic_List_T__ToArray))
                .AsMember(listOfT);

            // var __list = new List<elementType>();
            var listLocal = _factory.SynthesizedLocal(listOfT, syntax: node.Syntax);
            var listLocalExpr = _factory.Local(listLocal);

            var sideEffects = ArrayBuilder<BoundExpression>.GetInstance();

            // __list = new List<elementType>()
            sideEffects.Add(_factory.AssignmentExpression(
                listLocalExpr,
                new BoundObjectCreationExpression(
                    syntax: node.Syntax,
                    constructor: listCtor,
                    constructorsGroup: ImmutableArray.Create(listCtor),
                    arguments: ImmutableArray<BoundExpression>.Empty,
                    argumentNamesOpt: default,
                    argumentRefKindsOpt: default,
                    expanded: false,
                    argsToParamsOpt: default,
                    defaultArguments: default,
                    constantValueOpt: null,
                    initializerExpressionOpt: null,
                    wasTargetTyped: false,
                    type: listOfT)));

            // For each child: __list.Add(child) or __list.AddRange(spreadExpr)
            foreach (var child in loweredChildren)
            {
                if (IsSpreadChild(child, elementType))
                    sideEffects.Add(_factory.Call(listLocalExpr, listAddRange, child));
                else
                    sideEffects.Add(_factory.Call(listLocalExpr, listAdd, child));
            }

            // Result expression: __list.ToArray()
            var toArrayCall = _factory.Call(listLocalExpr, listToArray);

            return new BoundSequence(
                syntax: node.Syntax,
                locals: ImmutableArray.Create(listLocal),
                sideEffects: sideEffects.ToImmutableAndFree(),
                value: toArrayCall,
                type: toArrayCall.Type);
        }

        /// <summary>
        /// Returns true if <paramref name="child"/> is a spread expression —
        /// its type implements <c>IEnumerable&lt;elementType&gt;</c> rather than
        /// being a single <paramref name="elementType"/> value.
        /// </summary>
        private bool IsSpreadChild(BoundExpression child, TypeSymbol elementType)
        {
            var childType = child.Type;
            if (childType is null)
                return false;

            // A non-spread child's type equals elementType (single element).
            if (childType.Equals(elementType, TypeCompareKind.AllIgnoreOptions))
                return false;

            var ienumerableT = _compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T);

            // Check if it IS IEnumerable<elementType> directly.
            if (childType is NamedTypeSymbol named
                && named.OriginalDefinition.Equals(ienumerableT, TypeCompareKind.ConsiderEverything)
                && named.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.Length == 1)
            {
                var typeArg = named.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0].Type;
                if (typeArg.Equals(elementType, TypeCompareKind.AllIgnoreOptions))
                    return true;
            }

            // Check all implemented interfaces.
            foreach (var iface in childType.AllInterfacesNoUseSiteDiagnostics)
            {
                if (iface.OriginalDefinition.Equals(ienumerableT, TypeCompareKind.ConsiderEverything)
                    && iface.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.Length == 1)
                {
                    var typeArg = iface.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0].Type;
                    if (typeArg.Equals(elementType, TypeCompareKind.AllIgnoreOptions))
                        return true;
                }
            }

            return false;
        }
    }
}
