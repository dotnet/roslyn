// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using Microsoft.CodeAnalysis.FindReferences;

namespace Microsoft.CodeAnalysis.Editor
{
    internal abstract class FindReferencesContext
    {
        public virtual CancellationToken CancellationToken { get; }

        protected FindReferencesContext()
        {
        }

        public virtual void SetSearchLabel(string displayName)
        {
        }

        public virtual void OnCompleted()
        {
        }

        public virtual void OnDefinitionFound(DefinitionItem definition)
        {
        }

        public virtual void OnReferenceFound(SourceReferenceItem reference)
        {
        }

        public virtual void ReportProgress(int current, int maximum)
        {
        }
    }
}