// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


// csc /target:library Consumer.cs /r:TypeAndNamespaceDifferByCase.dll

public class TC1
    : SomeName.Dummy
{ 
}


public class TC2
    : somEnamE
{
}


public class TC3
    : somEnamE1
{
}

public class TC4
    : SomeName1
{
}

public class TC5
    : somEnamE2.OtherName
{
}

public class TC6
    : SomeName2.OtherName
{
}


public class TC7
    : NestingClass.somEnamE3
{
}

public class TC8
    : NestingClass.SomeName3
{
}
