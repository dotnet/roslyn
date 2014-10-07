using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Rename.ConflictEngine
{
    public struct InvocationExpressionCheckInfo
    {
        public readonly DocumentId DocumentId;

        // Span of the invocation expression identifier
        public readonly TextSpan IdentifierSpan;

        // The annotation that carries the information to check for conflict
        internal readonly RenameAnnotation RenameAnnotation;

        internal InvocationExpressionCheckInfo(DocumentId documentId, TextSpan identifierSpan, RenameAnnotation renameAnnotation)
        {
            this.DocumentId = documentId;
            this.IdentifierSpan = identifierSpan;
            this.RenameAnnotation = renameAnnotation;
        }
    }
}
