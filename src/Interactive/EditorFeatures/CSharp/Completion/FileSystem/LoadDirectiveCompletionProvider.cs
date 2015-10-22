// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.FileSystem;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.InteractiveWindow;
using Microsoft.VisualStudio.Text.Editor;

namespace Microsoft.CodeAnalysis.Editor.CSharp.Completion.FileSystem
{
    [ExportCompletionProvider("LoadDirectiveCompletionProvider", LanguageNames.CSharp)]
    // Using TextViewRole here is a temporary work-around to prevent this component from being loaded in
    // regular C# contexts.  We will need to remove this and implement a new "CSharp Script" Content type
    // in order to fix #load completion in .csx files (https://github.com/dotnet/roslyn/issues/5325).
    [TextViewRole(PredefinedInteractiveTextViewRoles.InteractiveTextViewRole)]
    internal partial class LoadDirectiveCompletionProvider : CompletionListProvider
    {
        private const string NetworkPath = "\\\\";
        private static readonly Regex s_directiveRegex = new Regex(@"#load\s+(""[^""]*""?)", RegexOptions.Compiled);

        public override async Task ProduceCompletionListAsync(CompletionListContext context)
        {
            var document = context.Document;
            var position = context.Position;
            var triggerInfo = context.TriggerInfo;
            var cancellationToken = context.CancellationToken;

            var text = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
            var items = GetItems(text, context.Document, position, triggerInfo, cancellationToken);

            context.AddItems(items);
        }

        public override bool IsTriggerCharacter(SourceText text, int characterPosition, OptionSet options)
        {
            return PathCompletionUtilities.IsTriggerCharacter(text, characterPosition);
        }

        private string GetPathThroughLastSlash(SourceText text, int position, Group quotedPathGroup)
        {
            return PathCompletionUtilities.GetPathThroughLastSlash(
                quotedPath: quotedPathGroup.Value,
                quotedPathStart: GetQuotedPathStart(text, position, quotedPathGroup),
                position: position);
        }

        private TextSpan GetTextChangeSpan(SourceText text, int position, Group quotedPathGroup)
        {
            return PathCompletionUtilities.GetTextChangeSpan(
                quotedPath: quotedPathGroup.Value,
                quotedPathStart: GetQuotedPathStart(text, position, quotedPathGroup),
                position: position);
        }

        private static int GetQuotedPathStart(SourceText text, int position, Group quotedPathGroup)
        {
            return text.Lines.GetLineFromPosition(position).Start + quotedPathGroup.Index;
        }

        private ImmutableArray<CompletionItem> GetItems(SourceText text, Document document, int position, CompletionTriggerInfo triggerInfo, CancellationToken cancellationToken)
        {
            var line = text.Lines.GetLineFromPosition(position);
            var lineText = text.ToString(TextSpan.FromBounds(line.Start, position));
            var match = s_directiveRegex.Match(lineText);
            if (!match.Success)
            {
                return ImmutableArray<CompletionItem>.Empty;
            }

            var quotedPathGroup = match.Groups[1];
            var quotedPath = quotedPathGroup.Value;
            var endsWithQuote = PathCompletionUtilities.EndsWithQuote(quotedPath);
            if (endsWithQuote && (position >= line.Start + match.Length))
            {
                return ImmutableArray<CompletionItem>.Empty;
            }

            var buffer = text.Container.GetTextBuffer();
            var snapshot = text.FindCorrespondingEditorTextSnapshot();
            if (snapshot == null)
            {
                return ImmutableArray<CompletionItem>.Empty;
            }

            var fileSystem = CurrentWorkingDirectoryDiscoveryService.GetService(snapshot);

            // TODO: https://github.com/dotnet/roslyn/issues/5263
            // Avoid dependency on a specific resolver.
            // The search paths should be provided by specialized workspaces:
            // - InteractiveWorkspace for interactive window 
            // - ScriptWorkspace for loose .csx files (we don't have such workspace today)
            var searchPaths = (document.Project.CompilationOptions.SourceReferenceResolver as SourceFileResolver)?.SearchPaths ?? ImmutableArray<string>.Empty;

            var helper = new FileSystemCompletionHelper(
                this,
                GetTextChangeSpan(text, position, quotedPathGroup),
                fileSystem,
                Glyph.OpenFolder,
                Glyph.CSharpFile,
                searchPaths: searchPaths,
                allowableExtensions: new[] { ".csx" },
                itemRules: ItemRules.Instance);

            var pathThroughLastSlash = this.GetPathThroughLastSlash(text, position, quotedPathGroup);

            return helper.GetItems(pathThroughLastSlash, documentPath: null);
        }
    }
}
