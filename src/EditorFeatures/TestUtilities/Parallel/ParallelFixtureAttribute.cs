// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;

namespace Roslyn.Test.Utilities.Parallel
{
    public class ParallelFixtureAttribute : RunWithAttribute
    {
        public ParallelFixtureAttribute() : base(typeof(ParallelTestClassCommand)) { }
    }
}
