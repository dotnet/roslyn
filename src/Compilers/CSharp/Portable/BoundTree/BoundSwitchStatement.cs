// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundSwitchStatement : IBoundSwitchStatement
    {
        BoundNode IBoundSwitchStatement.Value => this.Expression;
        ImmutableArray<BoundStatementList> IBoundSwitchStatement.Cases => StaticCast<BoundStatementList>.From(this.SwitchSections);
    }
}
