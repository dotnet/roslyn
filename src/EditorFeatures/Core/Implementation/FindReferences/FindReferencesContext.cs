// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using Microsoft.CodeAnalysis.Editor.Navigation;

namespace Microsoft.CodeAnalysis.Editor
{
    internal abstract class FindReferencesContext
    {
        public virtual CancellationToken CancellationToken { get; }

        protected FindReferencesContext()
        {
        }

        public virtual void OnStarted()
        {
        }

        public virtual void OnCompleted()
        {
        }

        public virtual void OnFindInDocumentStarted(Document document)
        {
        }

        public virtual void OnFindInDocumentCompleted(Document document)
        {
        }

        public virtual void OnDefinitionFound(
            INavigableItem definition, bool shouldDisplayWithNoReferences)
        {
        }

        public virtual void OnReferenceFound(INavigableItem definition, INavigableItem reference)
        {
        }

        public virtual void ReportProgress(int current, int maximum)
        {
        }
    }
}