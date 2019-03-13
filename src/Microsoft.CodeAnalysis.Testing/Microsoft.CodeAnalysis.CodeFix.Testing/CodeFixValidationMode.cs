// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Testing
{
    public enum CodeFixValidationMode
    {
        /// <summary>
        /// Code fix verification is limited to the raw text produced by the text.
        /// </summary>
        None,

        /// <summary>
        /// Code fix verification ensures that semantic structure of the tree produced by the code fix action matches
        /// the form produced by the compiler when parsing the text form of the document. Differences in trivia nodes,
        /// in particular the associativity of <see cref="SyntaxTrivia"/> to leading or trailing trivia lists, is
        /// ignored.
        /// </summary>
        /// <remarks>
        /// <para>Code fixes are generally expected to adhere to this validation mode.</para>
        /// </remarks>
        SemanticStructure,

        /// <summary>
        /// Code fix verification ensures that the tree produced by a code fix exactly matches the form of the tree
        /// produced by the compiler when parsing the text representation of the tree.
        /// </summary>
        Full,
    }
}
