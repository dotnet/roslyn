// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class Binder
    {
        // -----------------------------------------------------------------------
        // CSX binding
        //
        // Binding <Button Color="red">child</Button> proceeds in these steps:
        //
        //  1. Resolve the factory type from Options.CsxFactory.
        //  2. Find the nested CSX class on the factory and its Element interface.
        //  3. Resolve the tag name (Button) as an in-scope symbol.
        //  4. Verify the resolved symbol returns CSX.Element.
        //  5. Determine the props type (first parameter of the component method).
        //  6. Bind each attribute as a named property on the props type.
        //  7. Build a BoundObjectCreationExpression for new PropsType(Attr: val, ...).
        //  8. Resolve the CreateElement overload on the factory type.
        //  9. Bind children recursively; wrap text nodes via CSX.CreateTextNode.
        // 10. Return a BoundCsxElement.
        // -----------------------------------------------------------------------

        /// <summary>
        /// Binds a CSX element or self-closing element expression.
        /// </summary>
        internal BoundExpression BindCsxElement(CsxNodeSyntax node, BindingDiagnosticBag diagnostics)
        {
            // ---- Step 1: Resolve factory type ----
            // The CsxFactory is on CSharpParseOptions, accessible via the syntax tree.
            var parseOptions = node.SyntaxTree?.Options as CSharpParseOptions;
            var factoryTypeName = parseOptions?.CsxFactory;

            if (factoryTypeName == null)
            {
                diagnostics.Add(ErrorCode.ERR_CsxRequiresCsxFactory, node.Location);
                return BadExpression(node);
            }

            var factoryType = ResolveTypeByName(factoryTypeName, node, diagnostics);
            if (factoryType is null || factoryType.IsErrorType())
            {
                diagnostics.Add(ErrorCode.ERR_CsxFactoryTypeNotFound, node.Location, factoryTypeName);
                return BadExpression(node);
            }

            // ---- Step 2: Find CSX nested class and Element interface ----
            var csxClass = factoryType.GetTypeMembers("CSX").FirstOrDefault();
            if (csxClass is null)
            {
                diagnostics.Add(ErrorCode.ERR_CsxNamespaceNotFound, node.Location, factoryTypeName);
                return BadExpression(node);
            }

            var elementType = csxClass.GetTypeMembers("Element").FirstOrDefault() as TypeSymbol;
            if (elementType is null)
            {
                diagnostics.Add(ErrorCode.ERR_CsxElementTypeNotFound, node.Location, factoryTypeName);
                return BadExpression(node);
            }

            // ---- Step 3 & 4: Resolve tag name and validate return type ----
            NameSyntax tagName;
            SyntaxList<CsxAttributeSyntax> attributeList;

            if (node is CsxSelfClosingElementSyntax selfClosing)
            {
                tagName = selfClosing.Name;
                attributeList = selfClosing.Attributes;
            }
            else if (node is CsxElementSyntax element)
            {
                tagName = element.OpeningElement.Name;
                attributeList = element.OpeningElement.Attributes;
            }
            else
            {
                return BadExpression(node);
            }

            // Resolve the component name.
            // Always use BindingDiagnosticBag.Discarded so we suppress CS0119
            // ("type in expression context") and other irrelevant errors.
            // Crucially, AdjustIdentifierMapIfAny() inside BindIdentifier() fires
            // regardless of which bag is passed, so this call still sets flag 2 in
            // the identifier map — which is what assertBindIdentifierTargets requires.
            var componentExpr = this.BindExpression(tagName, BindingDiagnosticBag.Discarded);

            // Determine if the resolved expression returns CSX.Element
            MethodSymbol componentMethod = null;
            TypeSymbol propsType = null;

            if (componentExpr is BoundTypeExpression { Type: NamedTypeSymbol componentType }
                     && !componentType.IsErrorType())
            {
                // Tag resolved to a named type — look for the component method on it.
                MethodSymbol returnsElement = null;
                foreach (var member in componentType.GetMembers())
                {
                    if (member is MethodSymbol m
                        && m.IsStatic
                        && m.DeclaredAccessibility == Accessibility.Public
                        && m.ReturnType.Equals(elementType, TypeCompareKind.ConsiderEverything))
                    {
                        returnsElement = m;
                        if (IsValidRenderSignature(m, elementType))
                        {
                            componentMethod = m;
                            propsType = m.Parameters[0].Type;
                        }
                        break;
                    }
                }

                if (componentMethod is null)
                {
                    if (returnsElement is not null)
                    {
                        diagnostics.Add(ErrorCode.ERR_CsxRenderMethodInvalidSignature, tagName.Location,
                            returnsElement.ToDisplayString(), elementType.ToDisplayString());
                    }
                    else
                    {
                        diagnostics.Add(ErrorCode.ERR_CsxComponentNotReturningElement, tagName.Location,
                            tagName.ToString(), elementType.ToDisplayString());
                    }
                    return BadExpression(node);
                }
            }
            else
            {
                diagnostics.Add(ErrorCode.ERR_CsxUnknownComponent, tagName.Location, tagName.ToString());
                return BadExpression(node);
            }

            // ---- Step 5-7: Bind props ----
            BoundExpression propsArg = null;
            if (propsType is not null)
            {
                propsArg = BindCsxProps(node, attributeList, propsType, diagnostics);
            }
            else if (attributeList.Count > 0)
            {
                // Component takes no props but attributes were provided
                foreach (var attr in attributeList)
                {
                    diagnostics.Add(ErrorCode.ERR_CsxUnknownProp, attr.Location,
                        componentMethod.ToDisplayString(), attr.Name.Identifier.Text);
                }
            }

            // ---- Step 8: Resolve CreateElement overload on factory ----
            var createElementMethod = ResolveCreateElementMethod(factoryType, componentMethod, propsType, elementType, node, diagnostics);
            if (createElementMethod is null)
                return BadExpression(node);

            // ---- Step 9: Bind children ----
            var children = ImmutableArray<BoundExpression>.Empty;
            if (node is CsxElementSyntax elementWithChildren)
            {
                // Bind the closing tag name so the identifier map tracks it as fully bound.
                // We discard the result — we just need the side-effect of marking the identifier.
                var closingName = elementWithChildren.ClosingElement.Name;
                this.BindExpression(closingName, BindingDiagnosticBag.Discarded);

                var textNodeMethod = FindCreateTextNodeMethod(csxClass, factoryType, node, diagnostics);
                children = BindCsxChildren(elementWithChildren.Children, elementType, textNodeMethod, diagnostics);
            }

            // ---- Step 10: Return BoundCsxElement ----
            return new BoundCsxElement(
                syntax: node,
                type: elementType,
                factoryMethod: createElementMethod,
                componentMethod: componentMethod,
                componentArgument: componentExpr,
                propsArgument: propsArg,
                children: children);
        }

        /// <summary>
        /// Binds the attribute list of a CSX element into a props object creation expression.
        /// </summary>
        private BoundExpression BindCsxProps(
            CsxNodeSyntax node,
            SyntaxList<CsxAttributeSyntax> attributes,
            TypeSymbol propsType,
            BindingDiagnosticBag diagnostics)
        {
            // Find a constructor on propsType.
            // We look for a primary constructor (record) or any accessible constructor.
            var constructor = propsType.GetMembers(WellKnownMemberNames.InstanceConstructorName)
                .OfType<MethodSymbol>()
                .Where(m => m.DeclaredAccessibility == Accessibility.Public)
                .OrderByDescending(m => m.Parameters.Length)
                .FirstOrDefault();

            if (constructor is null)
            {
                diagnostics.Add(ErrorCode.ERR_CsxUnknownComponent, node.Location, propsType.ToDisplayString());
                return null;
            }

            // Build named arguments: map attribute name -> constructor parameter
            var args = ArrayBuilder<BoundExpression>.GetInstance();
            var argNames = ArrayBuilder<string>.GetInstance();
            var hasError = false;

            foreach (var attr in attributes)
            {
                var attrName = attr.Name.Identifier.Text;

                // Find matching constructor parameter
                var param = constructor.Parameters.FirstOrDefault(
                    p => string.Equals(p.Name, attrName, System.StringComparison.OrdinalIgnoreCase));

                if (param is null)
                {
                    diagnostics.Add(ErrorCode.ERR_CsxUnknownProp, attr.Location,
                        propsType.ToDisplayString(), attrName);
                    hasError = true;
                    continue;
                }

                BoundExpression valueExpr;
                if (attr.Value is null)
                {
                    // Boolean shorthand: Disabled => Disabled: true
                    valueExpr = new BoundLiteral(
                        attr.Name,
                        ConstantValue.True,
                        Compilation.GetSpecialType(SpecialType.System_Boolean));
                }
                else if (attr.Value is CsxExpressionSyntax csxExpr)
                {
                    valueExpr = this.BindValue(csxExpr.Expression, diagnostics, BindValueKind.RValue);
                }
                else
                {
                    valueExpr = this.BindValue(attr.Value, diagnostics, BindValueKind.RValue);
                }

                // Type check and apply implicit conversion so flow analysis never sees
                // unconverted nodes (e.g. UnconvertedInterpolatedString, DefaultLiteral,
                // method groups, local function groups — all have Type == null until converted).
                var useSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
                var conversion = this.Conversions.ClassifyConversionFromExpression(valueExpr, param.Type, isChecked: false, ref useSiteInfo);
                if (!conversion.IsValid && !conversion.IsImplicit)
                {
                    diagnostics.Add(ErrorCode.ERR_CsxPropTypeMismatch, attr.Location,
                        valueExpr.Type?.ToDisplayString() ?? "?", param.Type.ToDisplayString(), attrName);
                    hasError = true;
                    // Wrap in BadExpression so flow analysis doesn't assert WasConverted on the raw expr.
                    valueExpr = BadExpression(attr, valueExpr);
                }
                else if (valueExpr.NeedsToBeConverted() || valueExpr.Type is null)
                {
                    // NeedsToBeConverted() catches unconverted interpolated strings, default literals etc.
                    // Type == null catches method groups and local function groups, which have no type
                    // until converted to a concrete delegate type — the lowerer asserts if Type is null.
                    valueExpr = CreateConversion(valueExpr, conversion, param.Type, diagnostics);
                }

                args.Add(valueExpr);
                argNames.Add(param.Name);
            }

            // Check required (non-optional) parameters that weren't supplied
            foreach (var param in constructor.Parameters)
            {
                if (!param.IsOptional && !param.IsParams)
                {
                    bool supplied = argNames.Any(n =>
                        string.Equals(n, param.Name, System.StringComparison.OrdinalIgnoreCase));
                    if (!supplied)
                    {
                        diagnostics.Add(ErrorCode.ERR_CsxMissingRequiredProp, node.Location,
                            param.Name, param.Type.ToDisplayString());
                        hasError = true;
                    }
                }
            }

            if (hasError)
            {
                // Don't bail out — continue to build the BoundObjectCreationExpression with
                // hasErrors=true so the semantic model still has bound nodes for IDE navigation
                // (F12, hover, etc.) on the attributes that *were* successfully bound.
                // Diagnostics have already been reported above.
            }

            // Build argsToParamsOpt: for each supplied arg, record which parameter index it maps to.
            // This is required by BindDefaultArguments and by the lowerer's MakeArguments.
            var argsToParamsBuilder = ArrayBuilder<int>.GetInstance(argNames.Count);
            foreach (var name in argNames)
            {
                for (int i = 0; i < constructor.Parameters.Length; i++)
                {
                    if (string.Equals(constructor.Parameters[i].Name, name, System.StringComparison.OrdinalIgnoreCase))
                    {
                        argsToParamsBuilder.Add(i);
                        break;
                    }
                }
            }
            var argsToParamsOpt = argsToParamsBuilder.ToImmutableAndFree();

            // Fill in default values for optional parameters the user did not supply.
            // Skip when hasError — required params may be missing so BindDefaultArguments
            // would assert that unfilled params are optional (they may not be).
            BitVector defaultArguments;
            if (!hasError)
            {
                this.BindDefaultArguments(
                    node,
                    constructor.Parameters,
                    extensionReceiver: null,
                    argumentsBuilder: args,
                    argumentRefKindsBuilder: null,
                    namesBuilder: null,
                    argsToParamsOpt: ref argsToParamsOpt,
                    defaultArguments: out defaultArguments,
                    expanded: false,
                    enableCallerInfo: true,
                    diagnostics: diagnostics);
            }
            else
            {
                defaultArguments = BitVector.Empty;
            }

            var builtArgs = args.ToImmutableAndFree();
            argNames.Free();

            return new BoundObjectCreationExpression(
                syntax: node,
                constructor: constructor,
                constructorsGroup: ImmutableArray.Create(constructor),
                arguments: builtArgs,
                argumentNamesOpt: default,
                argumentRefKindsOpt: default,
                expanded: false,
                argsToParamsOpt: argsToParamsOpt,
                defaultArguments: defaultArguments,
                constantValueOpt: null,
                initializerExpressionOpt: null,
                wasTargetTyped: false,
                type: propsType,
                hasErrors: hasError);
        }

        /// <summary>
        /// Binds CSX child nodes into bound expressions.
        /// Text nodes are wrapped via <c>Factory.CSX.CreateTextNode(string)</c>.
        /// </summary>
        private ImmutableArray<BoundExpression> BindCsxChildren(
            SyntaxList<CsxNodeSyntax> children,
            TypeSymbol elementType,
            MethodSymbol textNodeMethod,
            BindingDiagnosticBag diagnostics)
        {
            if (children.Count == 0)
                return ImmutableArray<BoundExpression>.Empty;

            var builder = ArrayBuilder<BoundExpression>.GetInstance(children.Count);

            for (int i = 0; i < children.Count; i++)
            {
                var child = children[i];
                BoundExpression childExpr;

                if (child is CsxExpressionSyntax csxExpr)
                {
                    var boundExpr = this.BindValue(csxExpr.Expression, diagnostics, BindValueKind.RValue);

                    // Check if the expression is already a CSX.Element (or CSX.Element?) —
                    // ignore nullable annotation so {condition ? <A/> : null} works too.
                    var exprType = boundExpr.Type;
                    bool isElement = exprType is not null
                        && exprType.Equals(elementType, TypeCompareKind.AllIgnoreOptions);

                    if (isElement)
                    {
                        // Apply an identity conversion to satisfy the WasConverted debug assertion.
                        childExpr = GenerateConversionForAssignment(elementType, boundExpr, diagnostics);
                    }
                    else if (exprType is not null && IsEnumerableOfElement(exprType, elementType))
                    {
                        // Spread child: IEnumerable<elementType> or IEnumerable<elementType?>.
                        // Store as-is; the lowerer will emit a foreach loop to flatten it.
                        boundExpr.WasCompilerGenerated = true;
                        childExpr = boundExpr;
                    }
                    else if (exprType is not null
                        && GetSpecialTypeMember(SpecialMember.System_Object__ToString, BindingDiagnosticBag.Discarded, csxExpr) is MethodSymbol toStringMethod
                        && textNodeMethod is not null)
                    {
                        // Auto-stringify: synthesise expr.ToString() then CreateTextNode(result).
                        // Mark the receiver as compiler-generated so NeedsToBeConverted() returns
                        // false for BoundLocal/BoundParameter — avoids the WasConverted assertion
                        // without boxing the value type.
                        boundExpr.WasCompilerGenerated = true;

                        // Find the most-derived ToString() on the expression's own type to avoid
                        // boxing value types — fall back to object.ToString() if not found.
                        var ownToString = exprType.GetMembers(WellKnownMemberNames.ObjectToString)
                            .OfType<MethodSymbol>()
                            .FirstOrDefault(m => m.ParameterCount == 0 && !m.IsStatic)
                            ?? toStringMethod;

                        var stringExpr = BoundCall.Synthesized(
                            syntax: csxExpr,
                            receiverOpt: boundExpr,
                            initialBindingReceiverIsSubjectToCloning: ThreeState.False,
                            method: ownToString);

                        childExpr = BoundCall.Synthesized(
                            syntax: csxExpr,
                            receiverOpt: null,
                            initialBindingReceiverIsSubjectToCloning: ThreeState.False,
                            method: textNodeMethod,
                            arg0: stringExpr);
                    }
                    else
                    {
                        childExpr = BadExpression(child);
                    }
                }
                else if (child is CsxTextSyntax)
                {
                    // Accumulate all consecutive CsxTextSyntax siblings into one text run,
                    // then apply JSX-style whitespace normalisation.
                    // Note: horizontal whitespace from '}' trailing trivia is already emitted
                    // as a separate text node above; we only need to handle the text content itself.
                    bool prevWasExpr = i > 0 && children[i - 1] is CsxExpressionSyntax;

                    var sb = new StringBuilder();
                    while (i < children.Count && children[i] is CsxTextSyntax)
                    {
                        sb.Append(children[i].ToFullString());
                        i++;
                    }
                    i--; // outer loop will increment past the last consumed node

                    var normalised = NormaliseCsxWhitespace(sb.ToString(), trimFirstLine: !prevWasExpr);
                    if (string.IsNullOrEmpty(normalised))
                        continue;

                    if (textNodeMethod is not null)
                    {
                        var textLiteral = new BoundLiteral(
                            child,
                            ConstantValue.Create(normalised),
                            Compilation.GetSpecialType(SpecialType.System_String));

                        childExpr = BoundCall.Synthesized(
                            syntax: child,
                            receiverOpt: null,
                            initialBindingReceiverIsSubjectToCloning: ThreeState.False,
                            method: textNodeMethod,
                            arg0: textLiteral);
                    }
                    else
                    {
                        childExpr = BadExpression(child);
                    }
                }
                else if (child is CsxElementSyntax or CsxSelfClosingElementSyntax)
                {
                    childExpr = BindCsxElement((CsxNodeSyntax)child, diagnostics);
                }
                else
                {
                    childExpr = BadExpression(child);
                }

                builder.Add(childExpr);

                // After a {expr} child, emit any horizontal whitespace in the trailing trivia
                // of the closing '}' as a separate text node. This preserves the space in both
                // "{a} {b}" (next sibling is another expr) and "{a} text" (next sibling is text).
                if (child is CsxExpressionSyntax csxExprForTrivia && textNodeMethod is not null)
                {
                    var closeBraceTrivia = csxExprForTrivia.CloseBraceToken.TrailingTrivia;
                    var trailingSpace = new StringBuilder();
                    foreach (var trivia in closeBraceTrivia)
                    {
                        if (trivia.Kind() == SyntaxKind.WhitespaceTrivia)
                            trailingSpace.Append(trivia.ToFullString());
                        else
                            break;
                    }
                    if (trailingSpace.Length > 0)
                    {
                        var spaceLiteral = new BoundLiteral(
                            child,
                            ConstantValue.Create(trailingSpace.ToString()),
                            Compilation.GetSpecialType(SpecialType.System_String));
                        builder.Add(BoundCall.Synthesized(
                            syntax: child,
                            receiverOpt: null,
                            initialBindingReceiverIsSubjectToCloning: ThreeState.False,
                            method: textNodeMethod,
                            arg0: spaceLiteral));
                    }
                }
            }

            return builder.ToImmutableAndFree();
        }

        /// <summary>
        /// Applies JSX-style whitespace normalisation to a raw text run:
        /// <list type="bullet">
        ///   <item>Split on newlines</item>
        ///   <item>TrimStart each line (strip leading indentation)</item>
        ///   <item>Drop blank lines</item>
        ///   <item>Join surviving lines with a single space</item>
        /// </list>
        /// When <paramref name="trimFirstLine"/> is <c>false</c> (text immediately follows a
        /// <c>{expr}</c>), the first line's leading whitespace is preserved — it is meaningful
        /// spacing between the expression result and this text content.
        /// e.g. <c>"\n\t\t\tmy text\n\t\t\there\n\t\t\t"</c> → <c>"my text here"</c>
        /// </summary>
        private static string NormaliseCsxWhitespace(string raw, bool trimFirstLine = true)
        {
            var sb = new StringBuilder();
            var lines = raw.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                var trimmed = (i == 0 && !trimFirstLine) ? line.TrimEnd('\r') : line.TrimStart('\r', '\t', ' ');
                if (trimmed.Length == 0)
                    continue;
                if (sb.Length > 0)
                    sb.Append(' ');
                sb.Append(trimmed);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Resolves the <c>CreateElement</c> overload on the factory type that matches the
        /// given component method and props type.
        /// </summary>
        private MethodSymbol ResolveCreateElementMethod(
            NamedTypeSymbol factoryType,
            MethodSymbol componentMethod,
            TypeSymbol propsType,
            TypeSymbol elementType,
            SyntaxNode node,
            BindingDiagnosticBag diagnostics)
        {
            // Look for a method named CreateElement on the factory type.
            // Classic lowering target:
            //   static CSX.Element CreateElement<TProps>(Func<TProps, CSX.Element> component, TProps props, params CSX.Element[] children)
            // or simpler overloads without generics.
            var candidates = factoryType.GetMembers("CreateElement").OfType<MethodSymbol>()
                .Where(m => m.IsStatic && m.DeclaredAccessibility == Accessibility.Public)
                .ToList();

            if (candidates.Count == 0)
            {
                // Fall back: report error and use a placeholder
                diagnostics.Add(ErrorCode.ERR_CsxFactoryTypeNotFound, node.Location,
                    $"{factoryType.ToDisplayString()}.CreateElement");
                return null;
            }

            // Prefer the overload whose first parameter accepts the component's delegate type.
            // For now, pick the one with the most parameters as the best candidate.
            var best = candidates.OrderByDescending(m => m.Parameters.Length).First();
            return best;
        }

        /// <summary>
        /// Finds the <c>CSX.CreateTextNode(string)</c> method on the factory's nested CSX class.
        /// </summary>
        private MethodSymbol FindCreateTextNodeMethod(
            NamedTypeSymbol csxClass,
            NamedTypeSymbol factoryType,
            SyntaxNode node,
            BindingDiagnosticBag diagnostics)
        {
            var method = csxClass.GetMembers("CreateTextNode")
                .OfType<MethodSymbol>()
                .FirstOrDefault(m => m.IsStatic
                    && m.Parameters.Length == 1
                    && m.Parameters[0].Type.Equals(
                        Compilation.GetSpecialType(SpecialType.System_String),
                        TypeCompareKind.ConsiderEverything));

            if (method is null)
            {
                diagnostics.Add(ErrorCode.ERR_CsxCreateTextNodeNotFound, node.Location,
                    factoryType.ToDisplayString());
            }

            return method!;
        }

        /// <summary>
        /// Returns true if the method has the required render signature:
        /// <c>(TProps props, CSX.Element?[] children)</c> — exactly two parameters where
        /// the second is an array whose element type is the CSX element type (nullable or not).
        /// </summary>
        private bool IsValidRenderSignature(MethodSymbol method, TypeSymbol elementType)
        {
            if (method.Parameters.Length != 2)
                return false;

            var secondParam = method.Parameters[1].Type;
            if (secondParam is not ArrayTypeSymbol arr)
                return false;

            // Accept both Element[] and Element?[] — ignore nullable annotation on element type.
            return arr.ElementType.Equals(elementType, TypeCompareKind.AllIgnoreOptions);
        }

        /// <summary>
        /// Returns true if <paramref name="type"/> implements <c>IEnumerable&lt;elementType&gt;</c>
        /// or <c>IEnumerable&lt;elementType?&gt;</c> — i.e. it is a valid spread child sequence.
        /// </summary>
        private bool IsEnumerableOfElement(TypeSymbol type, TypeSymbol elementType)
        {
            var ienumerableT = Compilation.GetSpecialType(SpecialType.System_Collections_Generic_IEnumerable_T);
            if (ienumerableT.IsErrorType())
                return false;

            // Check the type itself and all its interfaces for IEnumerable<elementType>.
            foreach (var iface in type.AllInterfacesNoUseSiteDiagnostics)
            {
                if (iface.OriginalDefinition.Equals(ienumerableT, TypeCompareKind.ConsiderEverything)
                    && iface.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.Length == 1)
                {
                    var typeArg = iface.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0].Type;
                    if (typeArg.Equals(elementType, TypeCompareKind.AllIgnoreOptions))
                        return true;
                }
            }

            // Also check if type IS IEnumerable<elementType> directly.
            if (type is NamedTypeSymbol named
                && named.OriginalDefinition.Equals(ienumerableT, TypeCompareKind.ConsiderEverything)
                && named.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics.Length == 1)
            {
                var typeArg = named.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics[0].Type;
                if (typeArg.Equals(elementType, TypeCompareKind.AllIgnoreOptions))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Resolves a <see cref="NameSyntax"/> directly as a <see cref="NamedTypeSymbol"/>,
        /// without going through the expression binder (which would emit errors for types used
        /// in an expression context).
        /// </summary>
        private bool TryResolveTagAsType(NameSyntax name, out NamedTypeSymbol type)
        {
            type = null;
            string simpleName;
            if (name is IdentifierNameSyntax id)
                simpleName = id.Identifier.ValueText;
            else if (name is GenericNameSyntax gen)
                simpleName = gen.Identifier.ValueText;
            else
                return false;

            var lookupResult = LookupResult.GetInstance();
            var useSiteInfo = CompoundUseSiteInfo<AssemblySymbol>.Discarded;
            this.LookupSymbolsWithFallback(lookupResult, simpleName, arity: 0,
                useSiteInfo: ref useSiteInfo, options: LookupOptions.NamespacesOrTypesOnly);

            type = lookupResult.Symbols.FirstOrDefault() as NamedTypeSymbol;
            lookupResult.Free();
            return type is not null;
        }

        /// <summary>
        /// Resolves a fully-qualified type name (e.g. <c>"MyLib.H"</c>) to a <see cref="NamedTypeSymbol"/>.
        /// </summary>
        private NamedTypeSymbol ResolveTypeByName(string fullyQualifiedName, SyntaxNode node, BindingDiagnosticBag diagnostics)
        {
            var parts = fullyQualifiedName.Split('.');
            NamespaceOrTypeSymbol container = this.Compilation.GlobalNamespace;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                var ns = container.GetMembers(parts[i]).OfType<NamespaceOrTypeSymbol>().FirstOrDefault();
                if (ns is null)
                    return null;
                container = ns;
            }

            return container.GetTypeMembers(parts[parts.Length - 1]).FirstOrDefault();
        }
    }
}
