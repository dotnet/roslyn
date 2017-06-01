// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractInternalsVisibleToCompletionProvider : CommonCompletionProvider
    {
        private const string ProjectGuidKey = nameof(ProjectGuidKey);

        protected abstract bool IsPositionEntirelyWithinStringLiteral(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken);
        protected abstract string GetAttributeNameOfAttributeSyntaxNode(SyntaxNode attributeSyntaxNode);

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var cancellationToken = context.CancellationToken;
            var syntaxTree = await context.Document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            var syntaxFactsService = context.Document.GetLanguageService<ISyntaxFactsService>();
            if (IsPositionEntirelyWithinStringLiteral(syntaxTree, context.Position, cancellationToken))
            {
                var token = syntaxTree.FindTokenOnLeftOfPosition(context.Position, cancellationToken);
                var attributeSyntaxNode = GetAttributeSyntaxNodeOfToken(syntaxFactsService, token);
                if (attributeSyntaxNode == null)
                {
                    return;
                }

                var attributeName = GetAttributeNameOfAttributeSyntaxNode(attributeSyntaxNode);
                if (attributeName == "InternalsVisibleTo" || attributeName == nameof(InternalsVisibleToAttribute))
                {
                    if (await CheckTypeInfoOfAttributeAsync(context, attributeSyntaxNode).ConfigureAwait(false))
                    {
                        AddAssemblyCompletionItems(context, cancellationToken);
                    }
                }
            }
        }

        private static SyntaxNode GetAttributeSyntaxNodeOfToken(ISyntaxFactsService syntaxFactsService, SyntaxToken token)
        {
            //Supported cases:
            //[Attribute("|
            //[Attribute(parameterName:"Text|")
            //Also supported but excluded by syntaxTree.IsEntirelyWithinStringLiteral in ProvideCompletionsAsync
            //[Attribute(""|
            //[Attribute("Text"|)
            var node = token.Parent;
            if (syntaxFactsService.IsStringLiteralExpression(node))
            {
                // LiteralExpressionSyntax -> AttributeArgumentSyntax -> AttributeArgumentListSyntax -> AttributeSyntax
                var attributeSyntaxNodeCandidate = node.Parent?.Parent?.Parent;
                if (attributeSyntaxNodeCandidate != null && syntaxFactsService.IsAttribute(attributeSyntaxNodeCandidate))
                {
                    return attributeSyntaxNodeCandidate;
                }
            }

            return null;
        }

        private static async Task<bool> CheckTypeInfoOfAttributeAsync(CompletionContext context, SyntaxNode attributeNode)
        {
            var semanticModel = await context.Document.GetSemanticModelForNodeAsync(attributeNode, context.CancellationToken).ConfigureAwait(false);
            var typeInfo = semanticModel.GetTypeInfo(attributeNode);
            var type = typeInfo.Type;
            if (type == null)
            {
                return false;
            }

            //TODO there must be better ways to do the check type equality.
            var typeName = type.ToDisplayString(new SymbolDisplayFormat(typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));
            var assemblyName = type.ContainingAssembly.ToSignatureDisplayString();
            var internalsVisibleToQualifiedName = typeof(InternalsVisibleToAttribute).AssemblyQualifiedName;
            return typeName + ", " + assemblyName == internalsVisibleToQualifiedName;
        }

        private static void AddAssemblyCompletionItems(CompletionContext context, CancellationToken cancellationToken)
        {
            var currentProject = context.Document.Project;
            foreach (var project in context.Document.Project.Solution.Projects)
            {
                if (project == currentProject)
                {
                    continue;
                }

                var projectGuid = project.Id.Id.ToString();
                var completionItem = CommonCompletionItem.Create(
                    displayText: project.AssemblyName,
                    rules: CompletionItemRules.Default,
                    glyph: project.GetGlyph(),
                    properties: ImmutableDictionary.Create<string, string>().Add(ProjectGuidKey, projectGuid));
                context.AddItem(completionItem);
            }
        }

        public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey = default(char?), CancellationToken cancellationToken = default(CancellationToken))
        {
            if (item.Properties.TryGetValue(ProjectGuidKey, out var projectId))
            {
                var assemblyName = item.DisplayText;
                var project = document.Project.Solution.GetProject(ProjectId.CreateFromSerialized(new System.Guid(projectId)));
                var publicKey = await GetPublicKeyOfProjectAsync(project, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(publicKey))
                {
                    assemblyName += ", PublicKey=" + publicKey;
                }
                var textChange = new TextChange(item.Span, assemblyName);
                return CompletionChange.Create(textChange);
            }

            Debug.Fail("Project can't be found by projectId.");
            return await base.GetChangeAsync(document, item, commitKey, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<string> GetPublicKeyOfProjectAsync(Project project, CancellationToken cancellationToken)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (compilation.Assembly?.Identity?.IsStrongName ?? false)
            {
                return GetPublicKeyAsHexString(compilation.Assembly.Identity.PublicKey);
            }

            return string.Empty;
        }

        private static string GetPublicKeyAsHexString(ImmutableArray<byte> publicKey)
        {
            var pooledStrBuilder = PooledStringBuilder.GetInstance();
            var builder = pooledStrBuilder.Builder;
            foreach (var b in publicKey)
            {
                builder.Append(b.ToString("x2"));
            }
            return pooledStrBuilder.ToStringAndFree();
        }
    }
}