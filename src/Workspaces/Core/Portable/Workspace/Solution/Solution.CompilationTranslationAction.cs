// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    public partial class Solution
    {
        private abstract partial class CompilationTranslationAction
        {
            public abstract Task<Compilation> InvokeAsync(Compilation oldCompilation, CancellationToken cancellationToken);

            #region factories

            public static CompilationTranslationAction ProjectAssemblyName(string assemblyName)
            {
                return new ProjectAssemblyNameAction(assemblyName);
            }

            public static CompilationTranslationAction ProjectCompilationOptions(CompilationOptions options)
            {
                return new ProjectCompilationOptionsAction(options);
            }

            public static CompilationTranslationAction ProjectParseOptions(ProjectState state)
            {
                return new ProjectParseOptionsAction(state);
            }

            public static CompilationTranslationAction AddDocument(DocumentState state)
            {
                return new AddDocumentAction(state);
            }

            public static CompilationTranslationAction RemoveDocument(DocumentState state)
            {
                return new RemoveDocumentAction(state);
            }

            public static CompilationTranslationAction RemoveAllDocuments()
            {
                return new RemoveAllDocumentsAction();
            }

            public static CompilationTranslationAction TouchDocument(DocumentState oldState, DocumentState newState)
            {
                return new TouchDocumentAction(oldState, newState);
            }

            #endregion
        }
    }
}
