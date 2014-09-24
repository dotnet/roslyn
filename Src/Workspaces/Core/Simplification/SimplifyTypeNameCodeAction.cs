using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.Simplification
{
    internal class SimplifyTypeNameCodeAction : CodeAction.DocumentChangeAction
    {
        public SimplifyTypeNameCodeAction(string title, Func<CancellationToken, Task<Document>> createChangedDocument, string id)
            : base(title, createChangedDocument, id)
        {
        }
    }
}
