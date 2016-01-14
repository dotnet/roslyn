// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeFixes.GenerateMember;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.GenerateMember.GenerateConstructor;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.CSharp.CodeFixes.GenerateConstructor
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = PredefinedCodeFixProviderNames.GenerateConstructor), Shared]
    [ExtensionOrder(After = PredefinedCodeFixProviderNames.FullyQualify)]
    internal class GenerateConstructorCodeFixProvider : AbstractGenerateMemberCodeFixProvider
    {
        private const string CS0122 = "CS0122"; // CS0122: 'C' is inaccessible due to its protection level
        private const string CS1729 = "CS1729"; // CS1729: 'C' does not contain a constructor that takes n arguments
        private const string CS1739 = "CS1739"; // CS1739: The best overload for 'Program' does not have a parameter named 'v'
        private const string CS1503 = "CS1503"; // CS1503: Argument 1: cannot convert from 'T1' to 'T2'
        private const string CS7036 = "CS7036"; // CS7036: There is no argument given that corresponds to the required formal parameter 'v' of 'C.C(int)'

        public override ImmutableArray<string> FixableDiagnosticIds
        {
            get { return ImmutableArray.Create(CS0122, CS1729, CS1739, CS1503, CS7036); }
        }

        protected override Task<IEnumerable<CodeAction>> GetCodeActionsAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            var service = document.GetLanguageService<IGenerateConstructorService>();
            return service.GenerateConstructorAsync(document, node, cancellationToken);
        }

        protected override bool IsCandidate(SyntaxNode node, Diagnostic diagnostic)
        {
            if (node is SimpleNameSyntax || 
                node is ConstructorInitializerSyntax || 
                node is AttributeSyntax)
            {
                return true;
            }
            else if (node is ObjectCreationExpressionSyntax)
            {
                if (diagnostic.Id == CS1739)
                {
                    // Overload resolution failure" places the diagnostic on the constructor 
                    // instead of its arguments so we do not need any special handling.
                    return true;
                }
                else if (diagnostic.Id == CS1503)
                {
                    // CS1503 "Cannot convert from 'T1' to 'T2'" sets the span for the diagnostic around the argument itself
                    // We need to only return true if one of the arguments in our constructor exactly matches the span of the
                    // diagnostic
                    // example:
                    //       new X(new Y()) : Error cannot convert from 'Y' to 'T'
                    var arguments = (node as ObjectCreationExpressionSyntax).ArgumentList.Arguments;
                    if (arguments.Count > 0)
                    {
                        return arguments.Any(x => x.Span == diagnostic.Location.SourceSpan);
                    }
                    else
                    {
                        // There are no arguments, but we do not want to return true if the diagnostic
                        // has the same span because we want to run this check again on an ancestor node.
                        return node.Span != diagnostic.Location.SourceSpan;
                    }
                }
                else
                {
                    // Verify that we are not returning an outer constructor node, we should only offer to generate a constructor
                    // for the inner most constructor that does not exist.
                    // example:
                    //      new X(new Y())
                    //  If X already exists and accepts Y, but Y "does not contain a constructor that takes n arguments" we should only 
                    //  offer to generate Y, since Y is the only constructor with an error.
                    var arguments = (node as ObjectCreationExpressionSyntax).ArgumentList.Arguments;
                    if (arguments.Count > 0)
                    {
                        return !arguments.Any(
                            x => x.Expression is ObjectCreationExpressionSyntax && x.Span.Contains(diagnostic.Location.SourceSpan));
                    }
                    else
                    {
                        // No arguments to examine so we assume that current node is the best choice for generating a constructor.
                return true;
            }
                }
            }

            return diagnostic.Id == CS7036 && node is ClassDeclarationSyntax;
        }

        protected override SyntaxNode GetTargetNode(SyntaxNode node)
        {
            var objectCreationNode = node as ObjectCreationExpressionSyntax;
            if (objectCreationNode != null)
            {
                return objectCreationNode.Type.GetRightmostName();
            }

            var attributeNode = node as AttributeSyntax;
            if (attributeNode != null)
            {
                return attributeNode.Name;
            }

            return node;
        }
    }
}
