using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Services.Internal.Log;
using Roslyn.Utilities;

namespace Roslyn.Services
{
    public static partial class DocumentExtensions
    {
        /// <summary>
        /// Creates a new instance of this document updated to have the source code kind specified.
        /// </summary>
        public static Document UpdateSourceCodeKind(this Document document, SourceCodeKind kind)
        {
            return document.Project.Solution.WithDocumentSourceCodeKind(document.Id, kind).GetDocument(document.Id);
        }

        /// <summary>
        /// Creates a new instance of this document updated to have the text specified.
        /// </summary>
        public static Document UpdateText(this Document document, IText text)
        {
            return document.Project.Solution.WithDocumentText(document.Id, text).GetDocument(document.Id);
        }

        /// <summary>
        /// Creates a new instance of this document updated to have a syntax tree rooted by the specified syntax node.
        /// </summary>
        public static Document UpdateSyntaxRoot(this Document document, CommonSyntaxNode root)
        {
            return document.Project.Solution.WithDocumentSyntaxRoot(document.Id, root).GetDocument(document.Id);
        }

        /// <summary>
        /// Get the text changes between this document and a prior version of the same document.
        /// The changes, when applied to the text of the old document, will produce the text of the current document.
        /// </summary>
        public static IEnumerable<TextChange> GetTextChanges(this Document document, Document oldDocument, CancellationToken cancellationToken = default(CancellationToken))
        {
            using (Logger.LogBlock(FeatureId.Document, FunctionId.Document_GetTextChanges, d => d.Name, document, cancellationToken))
            {
                if (oldDocument == document)
                {
                    // no changes
                    return SpecializedCollections.EmptyEnumerable<TextChange>();
                }

                if (document.Id != oldDocument.Id)
                {
                    throw new ArgumentException("The specified document is not a version of this document.".NeedsLocalization());
                }

                // first try to see if text already knows its changes
                IList<TextChange> textChanges = null;

                IText text;
                IText oldText;
                if (document.TryGetText(out text) && oldDocument.TryGetText(out oldText))
                {
                    if (text == oldText)
                    {
                        return SpecializedCollections.EmptyEnumerable<TextChange>();
                    }

                    var container = text.Container;
                    if (container != null)
                    {
                        textChanges = text.GetTextChanges(oldText).ToList();

                        // if changes are significant (not the whole document being replaced) then use these changes
                        if (textChanges.Count > 1 || (textChanges.Count == 1 && textChanges[0].Span != new TextSpan(0, oldText.Length)))
                        {
                            return textChanges;
                        }
                    }
                }

                // get changes by diffing the trees
                CommonSyntaxTree tree = document.GetSyntaxTree(cancellationToken);
                CommonSyntaxTree oldTree = oldDocument.GetSyntaxTree(cancellationToken);

                return tree.GetChanges(oldTree);
            }
        }
    }
}