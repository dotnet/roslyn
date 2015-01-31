// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel
{
    internal partial class AbstractCodeModelService : ICodeModelService
    {
        protected abstract AbstractNodeLocator CreateNodeLocator();

        protected abstract class AbstractNodeLocator
        {
            private readonly AbstractCodeModelService _codeModelService;

            protected AbstractNodeLocator(AbstractCodeModelService codeModelService)
            {
                _codeModelService = codeModelService;
            }

            protected abstract EnvDTE.vsCMPart DefaultPart { get; }

            protected abstract VirtualTreePoint? GetStartPoint(SourceText text, SyntaxNode node, EnvDTE.vsCMPart part);
            protected abstract VirtualTreePoint? GetEndPoint(SourceText text, SyntaxNode node, EnvDTE.vsCMPart part);

            protected int GetTabSize(SourceText text)
            {
                return _codeModelService.GetTabSize(text);
            }

            public VirtualTreePoint? GetStartPoint(SyntaxNode node, EnvDTE.vsCMPart? part)
            {
                var text = node.SyntaxTree.GetText();
                return GetStartPoint(text, node, part ?? DefaultPart);
            }

            public VirtualTreePoint? GetEndPoint(SyntaxNode node, EnvDTE.vsCMPart? part)
            {
                var text = node.SyntaxTree.GetText();
                return GetEndPoint(text, node, part ?? DefaultPart);
            }
        }
    }
}
