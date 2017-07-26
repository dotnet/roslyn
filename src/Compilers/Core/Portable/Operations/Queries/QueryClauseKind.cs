// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Semantics
{
    /// <summary>
    /// Kind of <see cref="IQueryClause"/> within an <see cref="IQueryExpression"/>.
    /// </summary>
    public enum QueryClauseKind
    {
        /// <summary> Indicates an invalid clause kind.</summary>
        None = 0x00,
        /// <summary>Indicates an <see cref="IFromQueryClause"/> in C# and VB.</summary>
        FromClause = 0x01,
        /// <summary>Indicates an <see cref="ISelectQueryClause"/> in C# and VB.</summary>
        SelectClause = 0x02,
        /// <summary>Indicates an <see cref="IWhereQueryClause"/> in C# and VB.</summary>
        WhereClause = 0x03,
        /// <summary>Indicates an <see cref="ILetQueryClause"/> in C# and VB.</summary>
        LetClause = 0x04,
        /// <summary>Indicates an <see cref="IOrderByQueryClause"/> in C# and VB.</summary>
        OrderByClause = 0x05,
        /// <summary>Indicates an <see cref="GroupByQueryClause"/> in C# and VB.</summary>
        GroupByClause = 0x06,
        /// <summary>Indicates an <see cref="IGroupJoinQueryClause"/> in VB.</summary>
        GroupJoinClause = 0x07,
        /// <summary>Indicates an <see cref="IJoinQueryClause"/> in C# and VB.</summary>
        JoinClause = 0x08,
        /// <summary>Indicates an <see cref="IJoinIntoQueryClause"/> in C#.</summary>
        JoinIntoClause = 0x09,
        /// <summary>Indicates an <see cref="IDistinctQueryClause"/> in VB.</summary>
        DistinctClause = 0x0a,
        /// <summary>Indicates an <see cref="IAggregateQueryClause"/> in VB.</summary>
        AggregateClause = 0x0b,
        /// <summary>Indicates an <see cref="ISkipQueryClause"/> in VB.</summary>
        SkipClause = 0x0c,
        /// <summary>Indicates an <see cref="ISkipWhileQueryClause"/> in VB.</summary>
        SkipWhileClause = 0x0d,
        /// <summary>Indicates an <see cref="ITakeQueryClause"/> in VB.</summary>
        TakeClause = 0x0e,
        /// <summary>Indicates an <see cref="ITakeWhileQueryClause"/> in VB.</summary>
        TakeWhileClause = 0x0f,
    }
}

