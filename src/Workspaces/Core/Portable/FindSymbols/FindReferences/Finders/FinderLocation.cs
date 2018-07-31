// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


namespace Microsoft.CodeAnalysis.FindSymbols.Finders
{
    internal struct FinderLocation
    {
        public SyntaxNode Node;
        public ReferenceLocation Location;

        public FinderLocation(SyntaxNode node, ReferenceLocation location)
        {
            Node = node;
            Location = location;
        }

        public void Deconstruct(out SyntaxNode node, out ReferenceLocation location)
        {
            node = Node;
            location = Location;
        }
    }
}
