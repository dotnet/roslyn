// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Formatting.Rules;
using Microsoft.CodeAnalysis.Shared.Collections;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Formatting
{
    internal partial class FormattingContext : IIntervalIntrospector<FormattingContext.AnchorData>
    {
        /// <summary>
        /// data that will be used in an interval tree related to Anchor.
        /// </summary>
        private class AnchorData
        {
            private readonly AnchorIndentationOperation operation;

            public AnchorData(AnchorIndentationOperation operation, int originalColumn)
            {
                this.operation = operation;
                this.OriginalColumn = originalColumn;
            }

            public TextSpan TextSpan
            {
                get
                {
                    return this.operation.TextSpan;
                }
            }

            public SyntaxToken AnchorToken
            {
                get
                {
                    return this.operation.AnchorToken;
                }
            }

            public SyntaxToken StartToken
            {
                get
                {
                    return this.operation.StartToken;
                }
            }

            public SyntaxToken EndToken
            {
                get
                {
                    return this.operation.EndToken;
                }
            }

            public int OriginalColumn { get; private set; }
        }

        int IIntervalIntrospector<AnchorData>.GetStart(AnchorData value)
        {
            return value.TextSpan.Start;
        }

        int IIntervalIntrospector<AnchorData>.GetLength(AnchorData value)
        {
            return value.TextSpan.Length;
        }
    }
}