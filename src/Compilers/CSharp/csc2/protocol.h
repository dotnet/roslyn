// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

// NOTE: Changes to the protocol information in this file must also be synchronized with
// NOTE: Protocol.cs in the Roslyn.Compilers.BuildTasks project.

#pragma once

// The name of the server.
const WCHAR * const SERVERNAME = L"VBCSCompiler.exe";

// The name of the named pipe. A process id is appended to the end.
const WCHAR * const PIPENAME = L"VBCSCompiler";

// The id numbers below are just random. It's useful to use id numbers
// that won't occur accidentally for debugging.

// csc -- compile C#
const int REQUESTID_CSHARPCOMPILE = 0x44532521;

// vbc -- compiler VB
const int REQUESTID_VBCOMPILE = 0x44532522;

// analyzer -- analyze managed code
const int REQUESTID_ANALYZE = 0x44532523;

// Arugments for CSharp and VB Compiler

// The current directory of the client
const int ARGUMENTID_CURRENTDIRECTORY = 0x51147221;

// A comment line argument. The argument index indicates which one (0 .. N)
const int ARGUMENTID_COMMANDLINEARGUMENT = 0x51147222;

// The "LIB" environment variable of the client
const int ARGUMENTID_LIBENVVARIABLE = 0x51147223;




