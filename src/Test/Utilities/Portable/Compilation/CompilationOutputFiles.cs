// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Test.Utilities
{
    public struct CompilationOutputFiles
    {
        public readonly string PE;
        public readonly string Pdb;
        public readonly string XmlDocs;

        public CompilationOutputFiles(string pe, string pdb = null, string xmlDocs = null)
        {
            PE = pe;
            Pdb = pdb;
            XmlDocs = xmlDocs;
        }
    }
}
