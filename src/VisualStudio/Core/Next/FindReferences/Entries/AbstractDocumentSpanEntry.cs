// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Shell.TableManager;

namespace Microsoft.VisualStudio.LanguageServices.FindUsages
{
        internal abstract class AbstractDocumentSpanEntry : Entry
        {
            private readonly AbstractTableDataSourceFindUsagesContext _context;

            private readonly DocumentSpan _documentSpan;
            private readonly object _boxedProjectGuid;
            protected readonly SourceText _sourceText;

            protected AbstractDocumentSpanEntry(
                AbstractTableDataSourceFindUsagesContext context,
                RoslynDefinitionBucket definitionBucket,
                DocumentSpan documentSpan,
                Guid projectGuid,
                SourceText sourceText)
                : base(definitionBucket)
            {
                _context = context;

                _documentSpan = documentSpan;
                _boxedProjectGuid = projectGuid;
                _sourceText = sourceText;
            }

            protected StreamingFindUsagesPresenter Presenter => _context.Presenter;

            protected Document Document => _documentSpan.Document;
            protected TextSpan SourceSpan => _documentSpan.SourceSpan;

            protected override object GetValueWorker(string keyName)
            {
                switch (keyName)
                {
                    case StandardTableKeyNames.DocumentName:
                        return Document.FilePath;
                    case StandardTableKeyNames.Line:
                        return _sourceText.Lines.GetLinePosition(SourceSpan.Start).Line;
                    case StandardTableKeyNames.Column:
                        return _sourceText.Lines.GetLinePosition(SourceSpan.Start).Character;
                    case StandardTableKeyNames.ProjectName:
                        return Document.Project.Name;
                    case StandardTableKeyNames.ProjectGuid:
                        return _boxedProjectGuid;
                    case StandardTableKeyNames.Text:
                        return _sourceText.Lines.GetLineFromPosition(SourceSpan.Start).ToString().Trim();
                }

                return null;
            }
        }
}