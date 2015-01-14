// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


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
