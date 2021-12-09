// Copyright (c) SharpCrafters s.r.o. All rights reserved.
// This project is not open source. Please see the LICENSE.md file in the repository root for details.

namespace PostSharp.Engineering.BuildTools.Build.Model
{
    public class ProductDependency
    {
        public string Name { get; }

        public ProductDependency( string name )
        {
            this.Name = name;
        }
    }
}