// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

