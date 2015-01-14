// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

//csc /target:library Unavailable.cs

public class UnavailableClass { }
public class UnavailableClass<T> { }

public struct UnavailableStruct { }
public struct UnavailableStruct<T> { }

public interface UnavailableInterface { }
public interface UnavailableInterface<T> { }

public delegate void UnavailableDelegate();
public delegate void UnavailableDelegate<T>();
