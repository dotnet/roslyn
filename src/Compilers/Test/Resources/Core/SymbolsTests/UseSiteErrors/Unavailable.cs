// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//csc /target:library Unavailable.cs

public class UnavailableClass { }
public class UnavailableClass<T> { }

public struct UnavailableStruct { }
public struct UnavailableStruct<T> { }

public interface UnavailableInterface { }
public interface UnavailableInterface<T> { }

public delegate void UnavailableDelegate();
public delegate void UnavailableDelegate<T>();
