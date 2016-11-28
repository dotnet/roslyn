// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Telemetry
{
    internal class CompilationErrorDetailDiscoverer
    {
        /// <summary>
        /// name does not exist in context
        /// </summary>
        private const string CS0103 = "CS0103";

        /// <summary>
        /// type or namespace could not be found
        /// </summary>
        private const string CS0246 = "CS0246";

        /// <summary>
        /// wrong number of type args
        /// </summary>
        private const string CS0305 = "CS0305";

        /// <summary>
        /// The non-generic type 'A' cannot be used with type arguments
        /// </summary>
        private const string CS0308 = "CS0308";

        /// <summary>
        /// An attempt was made to use a non-attribute class in an attribute block. All the attribute types need to be inherited from System.Attribute
        /// </summary>
        private const string CS0616 = "CS0616";

        /// <summary>
        /// type does not contain a definition of method or extension method
        /// </summary>
        private const string CS1061 = "CS1061";

        /// <summary>
        /// The type of one argument in a method does not match the type that was passed when the class was instantiated. This error typically appears along with CS1502
        /// Likely to occur when a type / member exists in a new framework but its specific overload is missing.
        /// </summary>
        private const string CS1503 = "CS1503";

        /// <summary>
        /// cannot find implementation of query pattern
        /// </summary>
        private const string CS1935 = "CS1935";

        /// <summary>
        /// Used to record generic argument types the semantic model doesn't know about
        /// </summary>
        private const string UnknownGenericArgumentTypeName = "Unknown";

        /// <summary>
        /// Used to record symbol names for error scenarios where GetSymbolInfo/GetTypeInfo returns no symbol.
        /// </summary>
        private const string UnknownSymbolName = "Unknown";

        public async Task<List<CompilationErrorDetails>> GetCompilationErrorDetails(Document document, SyntaxNode bodyOpt, CancellationToken cancellationToken)
        {
            try
            {
                var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                ImmutableArray<Diagnostic> diagnostics;

                // If we have the SyntaxNode bodyOpt,
                // then we can use its fullSpan property to process a subset of the document.
                if (bodyOpt == null)
                {
                    diagnostics = semanticModel.GetDiagnostics(cancellationToken: cancellationToken);
                }
                else
                {
                    diagnostics = semanticModel.GetDiagnostics(bodyOpt.FullSpan, cancellationToken: cancellationToken);
                }

                if (diagnostics.Length == 0)
                {
                    return null;
                }

                var syntaxFacts = document.Project.LanguageServices.GetService<ISyntaxFactsService>();

                List<CompilationErrorDetails> ret = new List<CompilationErrorDetails>();

                foreach (var diagnostic in diagnostics)
                {
                    if (diagnostic.Severity != DiagnosticSeverity.Error || diagnostic.IsWarningAsError)
                    {
                        continue;
                    }

                    if (!IsTrackedCompilationError(diagnostic))
                    {
                        continue;
                    }

                    string errorDetailsFilename = diagnostic.Location.GetLineSpan().Path;
                    string errorDetailsUnresolvedMemberName = null;
                    string errorDetailsMethodName = null;
                    string errorDetailsLeftExpressionDocId = null;
                    string[] errorDetailsGenericArguments = null;
                    string[] errorDetailsArgumentTypes = null;
                    string[] errorDetailsLeftExpressionBaseTypeDocIds = null;

                    TextSpan span = diagnostic.Location.SourceSpan;

                    var node = root.FindNode(span);
                    if (node == null)
                    {
                        continue;
                    }

                    // If the expression binds, this could just be an extension method so we will continue and not
                    // log, as it's not actually an error.
                    if (ExpressionBinds(node, semanticModel, cancellationToken, checkForExtensionMethods: true))
                    {
                        continue;
                    }

                    // This is used to get unresolved member names.  It will not alway find a member name, but has been tested to do so 
                    // in the cases where the error code needs it.
                    string name;
                    int arity;
                    syntaxFacts.GetNameAndArityOfSimpleName(node, out name, out arity);
                    errorDetailsUnresolvedMemberName = name;

                    // Here we reuse the unresolved member name field for attribute classes that can't be resolved as
                    // actually being attributes.  Could factor this into a separate field for readability later.
                    AttributeSyntax attributeSyntax = node as AttributeSyntax;
                    if (attributeSyntax != null)
                    {
                        errorDetailsUnresolvedMemberName = semanticModel.GetTypeInfo(attributeSyntax, cancellationToken).Type.GetDocumentationCommentId();
                    }

                    GenericNameSyntax genericNameSyntax = node as GenericNameSyntax;
                    if (genericNameSyntax != null)
                    {
                        List<string> genericArgumentDocIds = new List<string>();
                        foreach (var genericArgument in genericNameSyntax.TypeArgumentList.Arguments)
                        {
                            var semanticInfo = semanticModel.GetTypeInfo(genericArgument, cancellationToken);
                            if (semanticInfo.Type != null)
                            {
                                genericArgumentDocIds.Add(GetDocId(semanticInfo.Type));
                            }
                            else
                            {
                                genericArgumentDocIds.Add(UnknownGenericArgumentTypeName);
                            }
                        }

                        errorDetailsGenericArguments = genericArgumentDocIds.ToArray();
                    }

                    ArgumentSyntax argumentSyntax = node as ArgumentSyntax;
                    if (argumentSyntax != null)
                    {
                        var argumentListSyntax = argumentSyntax.GetAncestor<BaseArgumentListSyntax>().Arguments;
                        var invocationExpression = argumentSyntax.GetAncestor<InvocationExpressionSyntax>();

                        errorDetailsArgumentTypes = (from argument in argumentListSyntax
                                                     select GetDocId(semanticModel.GetTypeInfo(argument.Expression, cancellationToken).Type)).ToArray();

                        if (invocationExpression != null)
                        {
                            var memberAccessExpression = invocationExpression.Expression as ExpressionSyntax;
                            var symbolInfo = semanticModel.GetSymbolInfo(memberAccessExpression, cancellationToken);

                            if (symbolInfo.CandidateSymbols.Length > 0)
                            {
                                // In this case, there is argument mismatch of some sort.
                                // Here we are getting the method name of any candidate symbols, then
                                // getting the docid of the type where the argument mismatch happened and storing this in LeftExpressionDocId.

                                errorDetailsMethodName = symbolInfo.CandidateSymbols.First().Name;
                                errorDetailsLeftExpressionDocId = GetDocId(symbolInfo.CandidateSymbols.First().ContainingType);
                            }
                        }
                    }

                    if (syntaxFacts.IsMemberAccessExpression(node.Parent))
                    {
                        var expression = node.Parent;

                        var leftExpression = syntaxFacts.GetExpressionOfMemberAccessExpression(expression);
                        if (leftExpression != null)
                        {
                            var semanticInfo = semanticModel.GetTypeInfo(leftExpression, cancellationToken);

                            var leftExpressionType = semanticInfo.Type;
                            if (leftExpressionType != null)
                            {
                                errorDetailsLeftExpressionDocId = GetDocId(leftExpressionType);

                                IEnumerable<string> baseTypeDocids = leftExpressionType.GetBaseTypes().Select(t => GetDocId(t));
                                errorDetailsLeftExpressionBaseTypeDocIds = baseTypeDocids.ToArray();
                            }
                        }
                    }

                    ret.Add(new CompilationErrorDetails(diagnostic.Id, errorDetailsFilename, errorDetailsMethodName, errorDetailsUnresolvedMemberName,
                        errorDetailsLeftExpressionDocId, errorDetailsLeftExpressionBaseTypeDocIds, errorDetailsGenericArguments, errorDetailsArgumentTypes));
                }

                return ret;
            }
            catch (Exception e)
            {
                List<CompilationErrorDetails> ret = new List<CompilationErrorDetails>();
                ret.Add(new CompilationErrorDetails("EXCEPTION", e.Message, e.StackTrace, null, null, null, null, null));
                return ret;
            }
        }

        private string GetDocId(ISymbol symbol)
        {
            if (symbol == null)
            {
                return UnknownSymbolName;
            }

            if (symbol is INamedTypeSymbol)
            {
                var typeSymbol = (INamedTypeSymbol)symbol;
                if (typeSymbol.IsGenericType && typeSymbol.OriginalDefinition != null)
                {
                    return typeSymbol.OriginalDefinition.GetDocumentationCommentId();
                }
            }
            else if (symbol is IMethodSymbol)
            {
                var methodSymbol = (IMethodSymbol)symbol;
                if (methodSymbol.IsGenericMethod && methodSymbol.OriginalDefinition != null)
                {
                    return methodSymbol.OriginalDefinition.GetDocumentationCommentId();
                }
            }

            return symbol.GetDocumentationCommentId();
        }

        private bool IsTrackedCompilationError(Diagnostic diagnostic)
        {
            return diagnostic.Id == CS0103 ||
                   diagnostic.Id == CS0246 ||
                   diagnostic.Id == CS0305 ||
                   diagnostic.Id == CS0308 ||
                   diagnostic.Id == CS0616 ||
                   diagnostic.Id == CS1061 ||
                   diagnostic.Id == CS1503 ||
                   diagnostic.Id == CS1935;
        }

        private bool ExpressionBinds(SyntaxNode expression, SemanticModel semanticModel, CancellationToken cancellationToken, bool checkForExtensionMethods = false)
        {
            // See if the name binds to something other then the error type. If it does, there's nothing further we need to do.
            // For extension methods, however, we will continue to search if there exists any better matched method.
            cancellationToken.ThrowIfCancellationRequested();
            var symbolInfo = semanticModel.GetSymbolInfo(expression, cancellationToken);
            if (symbolInfo.CandidateReason == CandidateReason.OverloadResolutionFailure && !checkForExtensionMethods)
            {
                return true;
            }

            return symbolInfo.Symbol != null;
        }
    }
}
