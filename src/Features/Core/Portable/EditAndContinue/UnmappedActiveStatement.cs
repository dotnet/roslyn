// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal readonly struct UnmappedActiveStatement
    {
        /// <summary>
        /// Unmapped span of the active statement
        /// (span within the file that contains #line directive that has an effect on the active statement, if there is any).
        /// </summary>
        public TextSpan UnmappedSpan { get; }

        /// <summary>
        /// Active statement - its <see cref="ActiveStatement.FileSpan"/> is mapped.
        /// </summary>
        public ActiveStatement Statement { get; }

        /// <summary>
        /// Mapped exception regions around the active statement.
        /// </summary>
        public ActiveStatementExceptionRegions ExceptionRegions { get; }

        public UnmappedActiveStatement(TextSpan unmappedSpan, ActiveStatement statement, ActiveStatementExceptionRegions exceptionRegions)
        {
            UnmappedSpan = unmappedSpan;
            Statement = statement;
            ExceptionRegions = exceptionRegions;
        }

        public void Deconstruct(out TextSpan unmappedSpan, out ActiveStatement statement, out ActiveStatementExceptionRegions exceptionRegions)
        {
            unmappedSpan = UnmappedSpan;
            statement = Statement;
            exceptionRegions = ExceptionRegions;
        }
    }
}
