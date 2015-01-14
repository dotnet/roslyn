// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// csc /target:library Module.cs

public class C
{
	public void MC1() { }
	public void MC2() { }
	
	event System.Action EC1;
	event System.Action EC2;
	event System.Action EC3;
}

public class D
{
	public void MD1() { }
	public int FD1;

	public int PE1 { get { return 1; } set { } }
	
	event System.Action ED1;
}

public class E
{
	public int FE1;
	public int FE2;
	public int FE3;
	public int FE4;
	
	public int PE1 { get { return 1; } set { } }
	public int PE2 { get { return 1; } set { } }
}
