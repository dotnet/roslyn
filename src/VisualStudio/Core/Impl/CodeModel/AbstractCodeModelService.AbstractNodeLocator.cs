// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    internal partial class AbstractCodeModelService : ICodeModelService
    {
        protected abstract AbstractNodeLocator CreateNodeLocator();

        protected abstract class AbstractNodeLocator
        {
            protected abstract string LanguageName { get; }

            protected abstract EnvDTE.vsCMPart DefaultPart { get; }

            protected abstract VirtualTreePoint? GetStartPoint(SourceText text, OptionSet options, SyntaxNode node, EnvDTE.vsCMPart part);
            protected abstract VirtualTreePoint? GetEndPoint(SourceText text, OptionSet options, SyntaxNode node, EnvDTE.vsCMPart part);

            protected int GetTabSize(OptionSet options)
            {
                return options.GetOption(FormattingOptions.TabSize, LanguageName);
            }

            public VirtualTreePoint? GetStartPoint(SyntaxNode node, OptionSet options, EnvDTE.vsCMPart? part)
            {
                var text = node.SyntaxTree.GetText();
                return GetStartPoint(text, options, node, part ?? DefaultPart);
            }

            public VirtualTreePoint? GetEndPoint(SyntaxNode node, OptionSet options, EnvDTE.vsCMPart? part)
            {
                var text = node.SyntaxTree.GetText();
                return GetEndPoint(text, options, node, part ?? DefaultPart);
            }
        }
    }
}
