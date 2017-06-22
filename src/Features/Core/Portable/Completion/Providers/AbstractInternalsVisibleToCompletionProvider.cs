// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractInternalsVisibleToCompletionProvider : CommonCompletionProvider
    {
        private const string ProjectGuidKey = nameof(ProjectGuidKey);

        protected abstract bool IsPositionEntirelyWithinStringLiteral(SyntaxTree syntaxTree, int position, CancellationToken cancellationToken);

        internal override bool IsInsertionTrigger(SourceText text, int insertedCharacterPosition, OptionSet options)
        {
            var ch = text[insertedCharacterPosition];
            return ch == '\"';
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var cancellationToken = context.CancellationToken;
            var syntaxTree = await context.Document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
            if (IsPositionEntirelyWithinStringLiteral(syntaxTree, context.Position, cancellationToken))
            {
                var syntaxFactsService = context.Document.GetLanguageService<ISyntaxFactsService>();
                var token = syntaxTree.FindTokenOnLeftOfPosition(context.Position, cancellationToken);
                var attributeSyntaxNode = GetAttributeSyntaxNodeOfToken(syntaxFactsService, token);
                if (attributeSyntaxNode == null)
                {
                    return;
                }

                if (await CheckTypeInfoOfAttributeAsync(context, attributeSyntaxNode).ConfigureAwait(false))
                {
                    await AddAssemblyCompletionItemsAsync(context, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private static SyntaxNode GetAttributeSyntaxNodeOfToken(ISyntaxFactsService syntaxFactsService, SyntaxToken token)
        {
            //Supported cases:
            //[Attribute("|
            //[Attribute(parameterName:"Text|")
            //Also supported but excluded by IsPositionEntirelyWithinStringLiteral in ProvideCompletionsAsync
            //[Attribute(""|
            //[Attribute("Text"|)
            var node = token.Parent;
            if (syntaxFactsService.IsStringLiteralExpression(node))
            {
                // Edge case: ElementAccessExpressionSyntax is present if the following statement is another attribute:
                //   [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("|
                //   [assembly: System.Reflection.AssemblyVersion("1.0.0.0")]
                //   [assembly: System.Reflection.AssemblyCompany("Test")]
                while (syntaxFactsService.IsElementAccessExpression(node.Parent))
                {
                    node = node.Parent;
                }

                // node -> AttributeArgumentSyntax -> AttributeArgumentListSyntax -> AttributeSyntax
                var attributeSyntaxNodeCandidate = node.Parent?.Parent?.Parent;
                if (syntaxFactsService.IsAttribute(attributeSyntaxNodeCandidate))
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

            var internalsVisibleToAttributeSymbol = semanticModel.Compilation.GetTypeByMetadataName(typeof(InternalsVisibleToAttribute).FullName);
            return type.Equals(internalsVisibleToAttributeSymbol);
        }

        private static async Task AddAssemblyCompletionItemsAsync(CompletionContext context, CancellationToken cancellationToken)
        {
            var currentProject = context.Document.Project;
            var currentCompilation = await currentProject.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            var currentAssemblyInfo = currentCompilation.Assembly;
            foreach (var project in context.Document.Project.Solution.Projects)
            {
                if (project == currentProject)
                {
                    continue;
                }

                if (await HasProjectAlreadyAccessToInternals(currentAssemblyInfo, project, cancellationToken).ConfigureAwait(false))
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

        private static async Task<bool> HasProjectAlreadyAccessToInternals(IAssemblySymbol sourceAssembly, Project project, CancellationToken cancellationToken)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            return sourceAssembly.GivesAccessTo(compilation.Assembly);
        }

        public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey = default(char?), CancellationToken cancellationToken = default(CancellationToken))
        {
            var projectIdGuid = item.Properties[ProjectGuidKey];
            var projectId = ProjectId.CreateFromSerialized(new System.Guid(projectIdGuid));
            var project = document.Project.Solution.GetProject(projectId);
            var assemblyName = item.DisplayText;
            var publicKey = await GetPublicKeyOfProjectAsync(project, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(publicKey))
            {
                assemblyName += ", PublicKey=" + publicKey;
            }

            var textChange = new TextChange(item.Span, assemblyName);
            return CompletionChange.Create(textChange);
        }

        private static async Task<string> GetPublicKeyOfProjectAsync(Project project, CancellationToken cancellationToken)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (compilation.Assembly?.Identity?.IsStrongName == true)
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