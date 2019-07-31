// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeFixes.Suppression
{
    internal abstract partial class AbstractSuppressionCodeFixProvider : IConfigurationFixProvider
    {
        internal abstract class AbstractGlobalSuppressMessageCodeAction : AbstractSuppressionCodeAction
        {
            private readonly Project _project;

            protected AbstractGlobalSuppressMessageCodeAction(AbstractSuppressionCodeFixProvider fixer, Project project)
                : base(fixer, title: FeaturesResources.in_Suppression_File)
            {
                _project = project;
            }

            protected sealed override async Task<IEnumerable<CodeActionOperation>> ComputeOperationsAsync(CancellationToken cancellationToken)
            {
                var changedSuppressionDocument = await GetChangedSuppressionDocumentAsync(cancellationToken).ConfigureAwait(false);
                return new CodeActionOperation[]
                {
                    new ApplyChangesOperation(changedSuppressionDocument.Project.Solution),
                    new OpenDocumentOperation(changedSuppressionDocument.Id, activateIfAlreadyOpen: true),
                    new DocumentNavigationOperation(changedSuppressionDocument.Id, position: 0)
                };
            }

            protected abstract Task<Document> GetChangedSuppressionDocumentAsync(CancellationToken cancellationToken);

            private string GetSuppressionsFilePath(string suppressionsFileName)
            {
                if (!string.IsNullOrEmpty(_project.FilePath))
                {
                    var fullPath = Path.GetFullPath(_project.FilePath);
                    var directory = PathUtilities.GetDirectoryName(fullPath);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        var suppressionsFilePath = PathUtilities.CombinePossiblyRelativeAndRelativePaths(directory, suppressionsFileName);
                        if (!string.IsNullOrEmpty(suppressionsFilePath))
                        {
                            return suppressionsFilePath;
                        }
                    }
                }

                return suppressionsFileName;
            }

            protected async Task<Document> GetOrCreateSuppressionsDocumentAsync(CancellationToken c)
            {
                var index = 1;
                var suppressionsFileName = s_globalSuppressionsFileName + Fixer.DefaultFileExtension;
                var suppressionsFilePath = GetSuppressionsFilePath(suppressionsFileName);

                Document suppressionsDoc = null;
                while (suppressionsDoc == null)
                {
                    var hasDocWithSuppressionsName = false;
                    foreach (var document in _project.Documents)
                    {
                        var filePath = document.FilePath;
                        var fullPath = !string.IsNullOrEmpty(filePath) ? Path.GetFullPath(filePath) : filePath;
                        if (fullPath == suppressionsFilePath)
                        {
                            // Existing global suppressions file. See if this file only has imports and global assembly
                            // attributes.
                            hasDocWithSuppressionsName = true;

                            var t = await document.GetSyntaxTreeAsync(c).ConfigureAwait(false);
                            var r = await t.GetRootAsync(c).ConfigureAwait(false);
                            if (r.ChildNodes().All(n => Fixer.IsImportsLine(n) || Fixer.IsAttributeListWithAssemblyAttributes(n)))
                            {
                                suppressionsDoc = document;
                                break;
                            }
                        }
                    }

                    if (suppressionsDoc == null)
                    {
                        if (hasDocWithSuppressionsName || File.Exists(suppressionsFilePath))
                        {
                            index++;
                            suppressionsFileName = s_globalSuppressionsFileName + index.ToString() + Fixer.DefaultFileExtension;
                            suppressionsFilePath = GetSuppressionsFilePath(suppressionsFileName);
                        }
                        else
                        {
                            // Create an empty global suppressions file.
                            suppressionsDoc = _project.AddDocument(suppressionsFileName, string.Empty);
                        }
                    }
                }

                return suppressionsDoc;
            }
        }
    }
}
