// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.LanguageServices;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal abstract class AbstractInternalsVisibleToCompletionProvider : CommonCompletionProvider
    {
        private const string ProjectGuidKey = nameof(ProjectGuidKey);

        protected abstract IImmutableList<SyntaxNode> GetAssemblyScopedAttributeSyntaxNodesOfDocument(SyntaxNode documentRoot);
        protected abstract SyntaxNode GetConstructorArgumentOfInternalsVisibleToAttribute(SyntaxNode internalsVisibleToAttribute);

        internal override bool IsInsertionTrigger(SourceText text, int insertedCharacterPosition, OptionSet options)
        {
            // Should trigger in these cases ($$ is the cursor position)
            // [InternalsVisibleTo($$         -> user enters "
            // [InternalsVisibleTo("$$")]     -> user enters any character
            var ch = text[insertedCharacterPosition];
            if (ch == '\"')
            {
                return true;
            }
            else
            {
                if (insertedCharacterPosition > 0)
                {
                    ch = text[insertedCharacterPosition - 1];
                    if (ch == '\"')
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            try
            {
                var cancellationToken = context.CancellationToken;
                var syntaxTree = await context.Document.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(false);
                var syntaxFactsService = context.Document.GetLanguageService<ISyntaxFactsService>();
                if (syntaxFactsService.IsEntirelyWithinStringOrCharOrNumericLiteral(syntaxTree, context.Position, cancellationToken))
                {
                    var token = syntaxTree.FindTokenOnLeftOfPosition(context.Position, cancellationToken);
                    var attributeSyntaxNode = GetAttributeSyntaxNodeOfToken(syntaxFactsService, token);
                    if (attributeSyntaxNode == null)
                    {
                        return;
                    }

                    if (await CheckTypeInfoOfAttributeAsync(context.Document, attributeSyntaxNode, context.CancellationToken).ConfigureAwait(false))
                    {
                        await AddAssemblyCompletionItemsAsync(context, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (Exception e) when (FatalError.ReportWithoutCrashUnlessCanceled(e))
            {
                // nop
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
            if (node != null && syntaxFactsService.IsStringLiteralExpression(node))
            {
                // Edge cases: 
                // ElementAccessExpressionSyntax is present if the following statement is another attribute:
                //   [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("|
                //   [assembly: System.Reflection.AssemblyVersion("1.0.0.0")]
                //   [assembly: System.Reflection.AssemblyCompany("Test")]
                // BinaryExpression is present if the string literal is concatenated:
                //   From: https://msdn.microsoft.com/de-de/library/system.runtime.compilerservices.internalsvisibletoattribute(v=vs.110).aspx
                //   [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Friend1, PublicKey=002400000480000094" + 
                //                                                                 "0000000602000000240000525341310004000" + ..
                while (syntaxFactsService.IsElementAccessExpression(node.Parent) || syntaxFactsService.IsBinaryExpression(node.Parent))
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

        private static async Task<bool> CheckTypeInfoOfAttributeAsync(Document document, SyntaxNode attributeNode, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelForNodeAsync(attributeNode, cancellationToken).ConfigureAwait(false);
            var typeInfo = semanticModel.GetTypeInfo(attributeNode);
            var type = typeInfo.Type;
            if (type == null)
            {
                return false;
            }

            var internalsVisibleToAttributeSymbol = semanticModel.Compilation.GetTypeByMetadataName(typeof(InternalsVisibleToAttribute).FullName);
            return type.Equals(internalsVisibleToAttributeSymbol);
        }

        private async Task AddAssemblyCompletionItemsAsync(CompletionContext context, CancellationToken cancellationToken)
        {
            var currentProject = context.Document.Project;
            var allInternalsVisibleToAttributesOfProject = await GetAllInternalsVisibleToAssemblyNamesOfProjectAsync(context, cancellationToken).ConfigureAwait(false);
            foreach (var project in context.Document.Project.Solution.Projects)
            {
                if (project == currentProject)
                {
                    continue;
                }

                if (IsProjectTypeUnsupported(project))
                {
                    continue;
                }

                if (allInternalsVisibleToAttributesOfProject.Contains(project.AssemblyName))
                {
                    continue;
                }

                var projectGuid = project.Id.Id.ToString();
                var completionItem = CommonCompletionItem.Create(
                    displayText: project.AssemblyName,
                    displayTextSuffix: "",
                    rules: CompletionItemRules.Default,
                    glyph: project.GetGlyph(),
                    properties: ImmutableDictionary.Create<string, string>().Add(ProjectGuidKey, projectGuid));
                context.AddItem(completionItem);
            }

            if (context.Items.Count > 0)
            {
                context.CompletionListSpan = await GetTextChangeSpanAsync(
                    context.Document, context.CompletionListSpan, cancellationToken).ConfigureAwait(false);
            }
        }

        private static bool IsProjectTypeUnsupported(Project project)
            => !project.SupportsCompilation;

        private async Task<IImmutableSet<string>> GetAllInternalsVisibleToAssemblyNamesOfProjectAsync(CompletionContext completionContext, CancellationToken cancellationToken)
        {
            // Looking up other InternalsVisibleTo attributes of this project. This is faster than compiling all projects of the solution and checking access via 
            // sourceAssembly.GivesAccessTo(compilation.Assembly)
            // at the cost of being not so precise (can't check the validity of the PublicKey).
            var project = completionContext.Document.Project;
            var resultBuilder = default(ImmutableHashSet<string>.Builder);
            foreach (var document in project.Documents)
            {
                var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var assemblyScopedAttributes = GetAssemblyScopedAttributeSyntaxNodesOfDocument(syntaxRoot);
                foreach (var attribute in assemblyScopedAttributes)
                {
                    // Skip attributes with errors. This skips the attribute that is currently edited, until it is complete:
                    // [assembly: InternalsVisibleTo("$$
                    // CS1003: Syntax error, ']' expected; CS1010: A string was not properly delimited; CS1026: An incomplete statement was found
                    // see also SyntaxNode.HasErrors
                    if (attribute.ContainsDiagnostics)
                    {
                        foreach (var diagnostic in attribute.GetDiagnostics())
                        {
                            if (diagnostic.Severity == DiagnosticSeverity.Error)
                            {
                                continue;
                            }
                        }
                    }

                    if (await CheckTypeInfoOfAttributeAsync(document, attribute, completionContext.CancellationToken).ConfigureAwait(false))
                    {
                        // See Microsoft.CodeAnalysis.PEAssembly.BuildInternalsVisibleToMap for reference on how
                        // the 'real' InternalsVisibleTo logic extracts and compares the assemblyName:
                        // * Extract the assemblyName by AssemblyIdentity.TryParseDisplayName
                        // * Compare with StringComparer.OrdinalIgnoreCase
                        // We take the same approach, but we do only a limited check of the PublicKey. 
                        // The PublicKey is checked by AssemblyIdentity.TryParseDisplayName to be 
                        // parseable (length, can be converted to bytes, etc.), but it is not tested whether 
                        // the public key actually fits to the assembly.
                        var assemblyName = await GetAssemblyNameFromInternalsVisibleToAttributeAsync(document, attribute, completionContext.CancellationToken).ConfigureAwait(false);
                        if (!string.IsNullOrWhiteSpace(assemblyName))
                        {
                            resultBuilder ??= ImmutableHashSet.CreateBuilder<string>(StringComparer.OrdinalIgnoreCase);
                            resultBuilder.Add(assemblyName);
                        }
                    }
                }
            }

            return resultBuilder == null
                ? ImmutableHashSet<string>.Empty
                : resultBuilder.ToImmutable();
        }

        private async Task<string> GetAssemblyNameFromInternalsVisibleToAttributeAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
        {
            var constructorArgument = GetConstructorArgumentOfInternalsVisibleToAttribute(node);
            if (constructorArgument == null)
            {
                return string.Empty;
            }

            var semanticModel = await document.GetSemanticModelForNodeAsync(constructorArgument, cancellationToken).ConfigureAwait(false);
            var constantCandidate = semanticModel.GetConstantValue(constructorArgument);
            if (constantCandidate is { HasValue: true, Value: string argument })
            {
                if (AssemblyIdentity.TryParseDisplayName(argument, out var assemblyIdentity))
                {
                    return assemblyIdentity.Name;
                }
            }

            return string.Empty;
        }

        private static async Task<TextSpan> GetTextChangeSpanAsync(Document document, TextSpan startSpan, CancellationToken cancellationToken)
        {
            var result = startSpan;
            var syntaxFacts = document.GetLanguageService<ISyntaxFactsService>();
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var token = root.FindToken(result.Start);
            if (syntaxFacts.IsStringLiteral(token) || syntaxFacts.IsVerbatimStringLiteral(token))
            {
                var text = root.GetText();

                // Expand selection in both directions until a double quote or any line break character is reached
                static bool IsWordCharacter(char ch) => !(ch == '"' || TextUtilities.IsAnyLineBreakCharacter(ch));

                result = CommonCompletionUtilities.GetWordSpan(
                    text, startSpan.Start, IsWordCharacter, IsWordCharacter, alwaysExtendEndSpan: true);
            }

            return result;
        }

        public override async Task<CompletionChange> GetChangeAsync(Document document, CompletionItem item, char? commitKey = default, CancellationToken cancellationToken = default)
        {
            var projectIdGuid = item.Properties[ProjectGuidKey];
            var projectId = ProjectId.CreateFromSerialized(new System.Guid(projectIdGuid));
            var project = document.Project.Solution.GetProject(projectId);
            var assemblyName = item.DisplayText;
            var publicKey = await GetPublicKeyOfProjectAsync(project, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(publicKey))
            {
                assemblyName += $", PublicKey={ publicKey }";
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
