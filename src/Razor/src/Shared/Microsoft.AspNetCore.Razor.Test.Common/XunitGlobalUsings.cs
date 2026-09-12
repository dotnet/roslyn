// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias XunitV3;

global using XunitV3::Xunit;
#pragma warning disable IDE0005 // Required by the linked shared test sources, but unused by some consumers.
global using XunitV3::Xunit.Sdk;
global using XunitV3::Xunit.v3;
#pragma warning restore IDE0005 // Using directive is unnecessary
