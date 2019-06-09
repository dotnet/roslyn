// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal partial class BoundSwitchSection : IBoundSwitchSection
    {
        ImmutableArray<BoundNode> IBoundSwitchSection.SwitchLabels => StaticCast<BoundNode>.From(this.SwitchLabels);
        ImmutableArray<BoundStatement> IBoundSwitchSection.Statements => this.Statements;
    }
}
