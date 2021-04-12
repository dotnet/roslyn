// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// csc /target:library TypeAndNamespaceDifferByCase.cs

namespace SomeName
{
public class Dummy
{}
}

public class somEnamE
{
}


public class somEnamE1
{
}

public class SomeName1
{
}

namespace somEnamE2
{
    public class OtherName
    {
        public class Nested
        {
        }
    }
}

namespace SomeName2
{
    public class OtherName
    {
        public class Nested
        {
        }
    }
}

public class NestingClass
{
    public class somEnamE3
    {
    }

    public class SomeName3
    {
    }
}
