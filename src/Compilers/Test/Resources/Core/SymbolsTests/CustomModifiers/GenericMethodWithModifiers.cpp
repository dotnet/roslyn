// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

//cl /clr:safe /LD GenericMethodWithModifiers.cpp

using namespace System;

public ref class CL1
{
public:
	generic <class T> where T : value class, ValueType
		virtual Nullable<T>^ Test(Nullable<T>^ x)
	{
		return x;
	}
};

public interface class I1
{
public:
	generic <class T> where T : value class, ValueType
		virtual Nullable<T>^ Test(Nullable<T>^ x);
};

