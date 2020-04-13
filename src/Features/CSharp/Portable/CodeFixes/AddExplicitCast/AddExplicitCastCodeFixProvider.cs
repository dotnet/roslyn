// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Internal.Log;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Simplification;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.AddExplicitCast
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.AddExplicitCast), Shared]
    internal sealed partial class AddExplicitCastCodeFixProvider : SyntaxEditorBasedCodeFixProvider
    {
        /// <summary>
        /// CS0266: Cannot implicitly convert from type 'x' to 'y'. An explicit conversion exists (are you missing a cast?)
        /// </summary>
        private const string CS0266 = nameof(CS0266);

        /// <summary>
        /// CS1503: Argument 1: cannot convert from 'x' to 'y'
        /// </summary>
        private const string CS1503 = nameof(CS1503);

        /// <summary>
        /// Give a set of least specific types with a limit, and the part exceeding the limit doesn't show any code fix, but logs telemetry 
        /// </summary>
        private const int MaximumConversionOptions = 3;

        [ImportingConstructor]
        [SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification = "Used in test code: https://github.com/dotnet/roslyn/issues/42814")]
        public AddExplicitCastCodeFixProvider()
        {
        }

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(CS0266, CS1503);

        internal override CodeFixCategory CodeFixCategory => CodeFixCategory.Compile;

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var diagnostic = context.Diagnostics.First();

            var root = await document.GetRequiredSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var syntaxFacts = document.GetRequiredLanguageService<ISyntaxFactsService>();
            var semanticModel = await document.GetRequiredSemanticModelAsync(cancellationToken).ConfigureAwait(false);

            var targetNode = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true)
                .GetAncestorsOrThis<ExpressionSyntax>().FirstOrDefault();
            if (targetNode == null)
                return;

            var hasSolution = TryGetTargetTypeInfo(
                semanticModel, root, diagnostic.Id, targetNode, cancellationToken,
                out var nodeType, out var potentialConversionTypes);
            if (!hasSolution)
            {
                return;
            }

            if (potentialConversionTypes.Length == 1)
            {
                context.RegisterCodeFix(new MyCodeAction(
                    CSharpFeaturesResources.Add_explicit_cast,
                    c => FixAsync(context.Document, context.Diagnostics.First(), c)),
                    context.Diagnostics);
            }
            else
            {
                var actions = ArrayBuilder<CodeAction>.GetInstance();

                // MaximumConversionOptions: we show at most [MaximumConversionOptions] options for this code fixer
                for (var i = 0; i < Math.Min(MaximumConversionOptions, potentialConversionTypes.Length); i++)
                {
                    var convType = potentialConversionTypes[i];
                    actions.Add(new MyCodeAction(
                        string.Format(
                            CSharpFeaturesResources.Convert_type_to_0,
                            convType.ToMinimalDisplayString(semanticModel, context.Span.Start)),
                        _ => ApplySingleConversionToDocumentAsync(document, ApplyFix(root, targetNode, convType))));
                }

                if (potentialConversionTypes.Length > MaximumConversionOptions)
                {
                    // If the number of potential conversion types is larger than options we could show, report telemetry
                    Logger.Log(FunctionId.CodeFixes_AddExplicitCast,
                        KeyValueLogMessage.Create(m =>
                        {
                            m["NumberOfCandidates"] = potentialConversionTypes.Length;
                        }));
                }

                context.RegisterCodeFix(new CodeAction.CodeActionWithNestedActions(
                    CSharpFeaturesResources.Add_explicit_cast,
                    actions.ToImmutableAndFree(), isInlinable: false),
                    context.Diagnostics);
            }
        }

        private static SyntaxNode ApplyFix(SyntaxNode currentRoot, ExpressionSyntax targetNode, ITypeSymbol conversionType)
        {
            // TODO:
            // the Simplifier doesn't remove the redundant cast from the expression
            // Issue link: https://github.com/dotnet/roslyn/issues/41500
            var castExpression = targetNode.Cast(conversionType).WithAdditionalAnnotations(Simplifier.Annotation);
            var newRoot = currentRoot.ReplaceNode(targetNode, castExpression);
            return newRoot;
        }

        private static Task<Document> ApplySingleConversionToDocumentAsync(Document document, SyntaxNode currentRoot)
            => Task.FromResult(document.WithSyntaxRoot(currentRoot));

        /// <summary>
        /// Output the current type information of the target node and the conversion type(s) that the target node is going to be cast by.
        /// Implicit downcast can appear on Variable Declaration, Return Statement, and Function Invocation
        /// <para/>
        /// For example:
        /// Base b; Derived d = [||]b;       
        /// "b" is the current node with type "Base", and the potential conversion types list which "b" can be cast by is {Derived}
        /// </summary>
        /// <param name="diagnosticId"> The ID of the diagnostic.</param>
        /// <param name="targetNode"> The node to be cast.</param>
        /// <param name="targetNodeType"> Output the type of "targetNode".</param>
        /// <param name="potentialConversionTypes"> Output the potential conversions types that "targetNode" can be cast to</param>
        /// <returns>
        /// True, if the target node has at least one potential conversion type, and they are assigned to "potentialConversionTypes"
        /// False, if the target node has no conversion type.
        /// </returns>
        private static bool TryGetTargetTypeInfo(
            SemanticModel semanticModel, SyntaxNode root, string diagnosticId, ExpressionSyntax targetNode,
            CancellationToken cancellationToken, [NotNullWhen(true)] out ITypeSymbol? targetNodeType,
            out ImmutableArray<ITypeSymbol> potentialConversionTypes)
        {
            potentialConversionTypes = ImmutableArray<ITypeSymbol>.Empty;

            var targetNodeInfo = semanticModel.GetTypeInfo(targetNode, cancellationToken);
            targetNodeType = targetNodeInfo.Type;

            if (targetNodeType == null)
                return false;

            // The error happens either on an assignement operation or on an invocation expression.
            // If the error happens on assignment operation, "ConvertedType" is different from the current "Type"
            using var _ = ArrayBuilder<ITypeSymbol>.GetInstance(out var mutablePotentialConversionTypes);
            if (diagnosticId == CS0266
                && targetNodeInfo.ConvertedType != null
                && !targetNodeType.Equals(targetNodeInfo.ConvertedType))
            {
                mutablePotentialConversionTypes.Add(targetNodeInfo.ConvertedType);
            }
            else if (diagnosticId == CS1503
                && targetNode.GetAncestorsOrThis<ArgumentSyntax>().FirstOrDefault() is ArgumentSyntax targetArgument
                && targetArgument.Parent is ArgumentListSyntax argumentList
                && argumentList.Parent is SyntaxNode invocationNode) // invocation node could be Invocation Expression, Object Creation, Base Constructor...
            {
                mutablePotentialConversionTypes.AddRange(GetPotentialConversionTypes(semanticModel, root, targetNodeType,
                    targetArgument, argumentList, invocationNode, cancellationToken));
            }

            // clear up duplicate types
            potentialConversionTypes = FilterValidPotentialConversionTypes(semanticModel, targetNode, targetNodeType,
                mutablePotentialConversionTypes);
            return !potentialConversionTypes.IsEmpty;

            static ImmutableArray<ITypeSymbol> GetPotentialConversionTypes(
                SemanticModel semanticModel, SyntaxNode root, ITypeSymbol targetNodeType, ArgumentSyntax targetArgument,
                ArgumentListSyntax argumentList, SyntaxNode invocationNode, CancellationToken cancellationToken)
            {
                // Implicit downcast appears on the argument of invocation node,
                // get all candidate functions and extract potential conversion types 
                var symbolInfo = semanticModel.GetSymbolInfo(invocationNode, cancellationToken);
                var candidateSymbols = symbolInfo.CandidateSymbols;

                using var _ = ArrayBuilder<ITypeSymbol>.GetInstance(out var mutablePotentialConversionTypes);
                foreach (var candidateSymbol in candidateSymbols.OfType<IMethodSymbol>())
                {
                    if (CanArgumentTypesBeConvertedToParameterTypes(
                            semanticModel, root, argumentList, candidateSymbol.Parameters, targetArgument,
                            cancellationToken, out var targetArgumentConversionType))
                    {
                        mutablePotentialConversionTypes.Add(targetArgumentConversionType);
                    }
                }

                // Sort the potential conversion types by inheritance distance, so that
                // operations are in order and user can choose least specific types(more accurate)
                mutablePotentialConversionTypes.Sort(new InheritanceDistanceComparer(semanticModel, targetNodeType));

                return mutablePotentialConversionTypes.ToImmutable();
            }

            static ImmutableArray<ITypeSymbol> FilterValidPotentialConversionTypes(
                SemanticModel semanticModel, ExpressionSyntax targetNode, ITypeSymbol targetNodeType,
                ArrayBuilder<ITypeSymbol> mutablePotentialConversionTypes)
            {
                using var _ = ArrayBuilder<ITypeSymbol>.GetInstance(out var validPotentialConversionTypes);
                foreach (var targetNodeConversionType in mutablePotentialConversionTypes)
                {
                    var commonConversion = semanticModel.Compilation.ClassifyCommonConversion(
                        targetNodeType, targetNodeConversionType);

                    // For cases like object creation expression. for example:
                    // Derived d = [||]new Base();
                    // It is always invalid except the target node has explicit conversion operator or is numeric.
                    if (targetNode.IsKind(SyntaxKind.ObjectCreationExpression)
                        && !commonConversion.IsUserDefined)
                    {
                        continue;
                    }

                    if (commonConversion.Exists)
                    {
                        validPotentialConversionTypes.Add(targetNodeConversionType);
                    }
                }
                return validPotentialConversionTypes.Distinct().ToImmutableArray();
            }
        }

        /// <summary>
        /// Test if all argument types can be converted to corresponding parameter types.
        /// </summary>
        /// For example:
        /// class Base { }
        /// class Derived1 : Base { }
        /// class Derived2 : Base { }
        /// class Derived3 : Base { }
        /// void DoSomething(int i, Derived1 d) { }
        /// void DoSomething(string s, Derived2 d) { }
        /// void DoSomething(int i, Derived3 d) { }
        /// 
        /// Base b;
        /// DoSomething(1, [||]b);
        ///
        /// *void DoSomething(string s, Derived2 d) { }* is not the perfect match candidate function for
        /// *DoSomething(1, [||]b)* because int and string are not ancestor-descendant relationship. Thus,
        /// Derived2 is not a potential conversion type.
        /// 
        /// <param name="argumentList"> The argument list of invocation expression</param>
        /// <param name="parameters"> The parameters of function</param>
        /// <param name="targetArgument"> The target argument that contains target node</param>
        /// <param name="targetArgumentConversionType"> Output the corresponding parameter type of
        /// the target arugment if function returns true</param>
        /// <returns>
        /// True, if arguments and parameters match perfectly. <paramref name="targetArgumentConversionType"/> Output the corresponding parameter type
        /// False, otherwise.
        /// </returns>
        private static bool CanArgumentTypesBeConvertedToParameterTypes(
            SemanticModel semanticModel, SyntaxNode root, ArgumentListSyntax argumentList,
            ImmutableArray<IParameterSymbol> parameters, ArgumentSyntax targetArgument,
            CancellationToken cancellationToken, [NotNullWhen(true)] out ITypeSymbol? targetArgumentConversionType)
        {
            targetArgumentConversionType = null;

            // No conversion happens under this case
            if (parameters.Length == 0)
                return false;

            var arguments = argumentList.Arguments;
            var newArguments = new List<ArgumentSyntax>();

            for (var i = 0; i < arguments.Count; i++)
            {
                // Parameter index cannot out of its range, #arguments is larger than #parameter only if the last parameter with keyword params
                var parameterIndex = Math.Min(i, parameters.Length - 1);

                // If the argument has a name, get the corresponding parameter index
                var nameSyntax = arguments[i].NameColon?.Name;
                if (nameSyntax != null)
                {
                    var name = nameSyntax.Identifier.ValueText;
                    if (!FindCorrespondingParameterByName(name, parameters, ref parameterIndex))
                        return false;
                }

                // The argument is either in order with parameters, or have a matched name with parameters.
                var argumentExpression = arguments[i].Expression;
                var parameterType = parameters[parameterIndex].Type;

                if (parameters[parameterIndex].IsParams
                    && parameters.Last().Type is IArrayTypeSymbol paramsType
                    && (semanticModel.ClassifyConversion(argumentExpression, paramsType.ElementType).Exists))
                {
                    newArguments.Add(arguments[i].WithExpression(argumentExpression.Cast(paramsType.ElementType)));

                    if (arguments[i].Equals(targetArgument))
                        targetArgumentConversionType = paramsType.ElementType;
                }
                else if (semanticModel.ClassifyConversion(argumentExpression, parameterType).Exists)
                {
                    newArguments.Add(arguments[i].WithExpression(argumentExpression.Cast(parameterType)));

                    if (arguments[i].Equals(targetArgument))
                        targetArgumentConversionType = parameterType;
                }
                else if (argumentExpression.Kind() == SyntaxKind.DeclarationExpression
                    && semanticModel.GetTypeInfo(argumentExpression, cancellationToken).Type is ITypeSymbol argumentType
                    && semanticModel.Compilation.ClassifyCommonConversion(argumentType, parameterType).IsIdentity)
                {
                    // Direct conversion from a declaration expression to a type is unspecified, thus we classify the
                    // conversion from the type of declaration expression to the parameter type
                    // An example for this case:
                    // void Foo(out int i) { i = 1; }
                    // Foo([|out var i|]);
                    // "var i" is a declaration expression
                    // 
                    // In addition, since this case is with keyword "out", the type of declaration expression and the
                    // parameter type must be identical in order to match.
                    newArguments.Add(arguments[i]);
                }
                else
                {
                    return false;
                }
            }

            return targetArgumentConversionType != null
                && IsInvocationExpressionWithNewArgumentsApplicable(semanticModel, root, argumentList, newArguments, targetArgument);
        }

        private static bool FindCorrespondingParameterByName(
            string argumentName, ImmutableArray<IParameterSymbol> parameters, ref int parameterIndex)
        {
            for (var j = 0; j < parameters.Length; j++)
            {
                if (argumentName.Equals(parameters[j].Name))
                {
                    parameterIndex = j;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check whether the invocation expression with new arguments is applicatble.
        /// </summary>
        /// <param name="oldArgumentList" >old argumentList node</param>
        /// <param name="newArguments"> new arguments that are cast by corresponding parameter types</param>
        /// <param name="targetNode"> The node needs to be cast.</param>
        /// <returns>
        /// Return true if the invocation expression with new arguments is applicatble.
        /// Otherwise, return false
        /// </returns>
        private static bool IsInvocationExpressionWithNewArgumentsApplicable(
            SemanticModel semanticModel, SyntaxNode root, ArgumentListSyntax oldArgumentList,
            List<ArgumentSyntax> newArguments, SyntaxNode targetNode)
        {
            var separatedSyntaxList = SyntaxFactory.SeparatedList(newArguments);
            var newRoot = root.ReplaceNode(oldArgumentList, oldArgumentList.WithArguments(separatedSyntaxList));

            var newArgumentListNode = newRoot.FindNode(targetNode.Span).GetAncestorsOrThis<ArgumentListSyntax>().FirstOrDefault();
            if (newArgumentListNode.Parent is SyntaxNode newExpression)
            {
                var symbolInfo = semanticModel.GetSpeculativeSymbolInfo(newExpression.SpanStart, newExpression,
                    SpeculativeBindingOption.BindAsExpression);
                return symbolInfo.Symbol != null;
            }
            return false;
        }

        protected override async Task FixAllAsync(
            Document document, ImmutableArray<Diagnostic> diagnostics,
            SyntaxEditor editor, CancellationToken cancellationToken)
        {
            var root = await document.GetRequiredSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var targetNodes = diagnostics.SelectAsArray(
                d => root.FindNode(d.Location.SourceSpan, getInnermostNodeForTie: true)
                         .GetAncestorsOrThis<ExpressionSyntax>().FirstOrDefault());

            await editor.ApplyExpressionLevelSemanticEditsAsync(
                document, targetNodes,
                (semanticModel, targetNode) => true,
                (semanticModel, currentRoot, targetNode) =>
                {
                    // All diagnostics have the same error code
                    if (TryGetTargetTypeInfo(semanticModel, currentRoot, diagnostics[0].Id, targetNode,
                        cancellationToken, out var nodeType, out var potentialConversionTypes)
                        && potentialConversionTypes.Length == 1)
                    {
                        return ApplyFix(currentRoot, targetNode, potentialConversionTypes[0]);
                    }

                    return currentRoot;
                },
                cancellationToken).ConfigureAwait(false);
        }

        private class MyCodeAction : CodeAction.DocumentChangeAction
        {
            public MyCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument)
                : base(title, createChangedDocument, equivalenceKey: title)
            {
            }
        }
    }
}
