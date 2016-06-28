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
    [ExportCompletionProviderMef1("LoadDirectiveCompletionProvider", LanguageNames.CSharp)]
    // Using TextViewRole here is a temporary work-around to prevent this component from being loaded in
    // regular C# contexts.  We will need to remove this and implement a new "CSharp Script" Content type
    // in order to fix #load completion in .csx files (https://github.com/dotnet/roslyn/issues/5325).
    [TextViewRole(PredefinedInteractiveTextViewRoles.InteractiveTextViewRole)]
    internal partial class LoadDirectiveCompletionProvider : CommonCompletionProvider
    {
        private const string NetworkPath = "\\\\";
        private static readonly Regex s_directiveRegex = new Regex(@"#load\s+(""[^""]*""?)", RegexOptions.Compiled);

        private static readonly ImmutableArray<CharacterSetModificationRule> s_commitRules =
            ImmutableArray.Create(CharacterSetModificationRule.Create(CharacterSetModificationKind.Replace, '"', '\\', ','));

        private static readonly CompletionItemRules s_rules = CompletionItemRules.Create(commitCharacterRules: s_commitRules);

        public override async Task ProvideCompletionsAsync(CompletionContext context)
        {
            var text = await context.Document.GetTextAsync(context.CancellationToken).ConfigureAwait(false);
            var items = GetItems(text, context.Document, context.Position, context.Trigger, context.CancellationToken);
            context.AddItems(items);
        }

        internal override bool IsInsertionTrigger(SourceText text, int characterPosition, OptionSet options)
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

        private ImmutableArray<CompletionItem> GetItems(SourceText text, Document document, int position, CompletionTrigger triggerInfo, CancellationToken cancellationToken)
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
                itemRules: s_rules);

            var pathThroughLastSlash = this.GetPathThroughLastSlash(text, position, quotedPathGroup);

            return helper.GetItems(pathThroughLastSlash, documentPath: null);
        }

        protected override Task<TextChange?> GetTextChangeAsync(CompletionItem selectedItem, char? ch, CancellationToken cancellationToken)
        {
            // When we commit "\\" when the user types \ we have to adjust for the fact that the
            // controller will automatically append \ after we commit.  Because of that, we don't
            // want to actually commit "\\" as we'll end up with "\\\".  So instead we just commit
            // "\" and know that controller will append "\" and give us "\\".
            if (selectedItem.DisplayText == NetworkPath && ch == '\\')
            {
                return Task.FromResult<TextChange?>(new TextChange(selectedItem.Span, "\\"));
            }

            return base.GetTextChangeAsync(selectedItem, ch, cancellationToken);
        }
    }
}