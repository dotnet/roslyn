using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.WorkspaceServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public static class SolutionExtensions
    {
        /// <summary>
        /// Creates a new solution instance with the project updated to include a new document that
        /// will load its text from the file path.
        /// </summary>
        public static Solution AddDocument(this Solution solution, DocumentId documentId, string filePath, IEnumerable<string> folders = null)
        {
            return solution.AddDocument(documentId, Path.GetFileName(filePath), new FileTextLoader(filePath), folders);
        }
    }
}