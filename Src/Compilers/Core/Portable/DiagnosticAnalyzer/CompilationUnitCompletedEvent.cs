// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    public sealed class CompilationUnitCompletedEvent : CompilationEvent
    {
        public CompilationUnitCompletedEvent(Compilation compilation, SyntaxTree compilationUnit) : base(compilation)
        {
            this.CompilationUnit = compilationUnit;
        }
        private WeakReference<SemanticModel> weakModel = null;
        public SemanticModel SemanticModel
        {
            get
            {
                var weakModel = this.weakModel;
                SemanticModel semanticModel;
                if (weakModel == null || !weakModel.TryGetTarget(out semanticModel))
                {
                    semanticModel = Compilation.GetSemanticModel(CompilationUnit);
                    this.weakModel = new WeakReference<SemanticModel>(semanticModel);
                }
                return semanticModel;
            }
            private set
            {
                this.weakModel = new WeakReference<SemanticModel>(value);
            }
        }
        override public void FlushCache()
        {
        }

        public SyntaxTree CompilationUnit { get; private set; }
        public override string ToString()
        {
            return "CompilationUnitCompletedEvent(" + CompilationUnit.FilePath + ")";
        }
    }
}
