// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    internal class ExtractMethodResult
    {
        /// <summary>
        /// True if the extract method operation succeeded.
        /// </summary>
        public bool Succeeded { get; }

        /// <summary>
        /// True if the extract method operation is possible if the original span is adjusted.
        /// </summary>
        public bool SucceededWithSuggestion { get; }

        /// <summary>
        /// The transformed document that was produced as a result of the extract method operation.
        /// </summary>
        public Document Document { get; }

        /// <summary>
        /// The reasons why the extract method operation did not succeed.
        /// </summary>
        public IEnumerable<string> Reasons { get; }

        /// <summary>
        /// the generated method node that contains the extracted code.
        /// </summary>
        public SyntaxNode MethodDeclarationNode { get; }

        /// <summary>
        /// The name token for the invocation node that replaces the extracted code.
        /// </summary>
        public SyntaxToken InvocationNameToken { get; }

        internal ExtractMethodResult(
            OperationStatusFlag status,
            IEnumerable<string> reasons,
            Document document,
            SyntaxToken invocationNameToken,
            SyntaxNode methodDeclarationNode)
        {
            Status = status;

            Succeeded = status.Succeeded() && !status.HasSuggestion();
            SucceededWithSuggestion = status.Succeeded() && status.HasSuggestion();

            Reasons = (reasons ?? SpecializedCollections.EmptyEnumerable<string>()).ToReadOnlyCollection();

            Document = document;
            InvocationNameToken = invocationNameToken;
            MethodDeclarationNode = methodDeclarationNode;
        }

        /// <summary>
        /// internal status of result. more fine grained reason why it is failed. 
        /// </summary>
        internal OperationStatusFlag Status { get; }
    }
}
