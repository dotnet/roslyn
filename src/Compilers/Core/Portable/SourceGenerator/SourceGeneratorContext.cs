// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis
{
    public abstract class SourceGeneratorContext 
    {
        public abstract Compilation Compilation { get; }
        public abstract void ReportDiagnostic(Diagnostic diagnostic);
        public abstract void AddCompilationUnit(string name, SyntaxTree tree);
    }
}
