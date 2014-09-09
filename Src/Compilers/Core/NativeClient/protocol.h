// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  
// Licensed under the Apache License, Version 2.0.  
// See License.txt in the project root for license information.

#pragma once

#include <memory>
#include <vector>
#include <string>
#include "pipe_utils.h"

using namespace std;

// The version of the protocol
const int PROTOCOL_VERSION = 1;

// The id numbers below are just random. It's useful to use id numbers
// that won't occur accidentally for debugging.
enum RequestLanguage {
// csc -- compile C#
REQUESTID_CSHARPCOMPILE = 0x44532521,
// vbc -- compiler VB
REQUESTID_VBCOMPILE = 0x44532522,
};
// Arguments for CSharp and VB Compiler

enum ArgumentId {
// The current directory of the client
ARGUMENTID_CURRENTDIRECTORY = 0x51147221,
// A comment line argument. The argument index indicates which one (0 .. N)
ARGUMENTID_COMMANDLINEARGUMENT = 0x51147222,
// The "LIB" environment variable of the client
ARGUMENTID_LIBENVVARIABLE = 0x51147223,
};

// Class representing a compilation request to be sent to the server.
// The request structure is as follows:
// 
// Field name       Type            Size (bytes)
// ---------------------------------------------
// Version          int             4
// Language         RequestLanguage 4
// Arguments        Argument[]      variable
//
// The Argument structure is as follows:
// 
// Field name       Type            Size (bytes)
// ---------------------------------------------
// Id               int             4
// Index            int             4
// Value            wchar_t[]       variable
class Request
{
public:
	int protocolVersion;
	RequestLanguage language;
	bool utf8Output;

	struct Argument {
		ArgumentId id;
		int index;
		wstring value;

		Argument(ArgumentId id, int index, LPCWSTR value)
			: id(id), index(index), value(value)
		{}

		bool operator==(const Request::Argument& right) const
		{
			return this->id == right.id
				&& this->index == right.index
				&& this->value == right.value;
		}
	};
	vector<Argument> arguments;

	Request(Request&& other);
	Request(int version,
		    RequestLanguage language,
            bool utf8Output,
			vector<Argument>&& arguments);

	Request& operator=(Request&& other);

	// Write the request buffer to the pipe, prefixed by its length.
    // This procedure either succeeds or logs an error and exits the process.
	bool WriteToPipe(IPipe&);
};

Request CreateRequest(
	RequestLanguage,
	LPCWSTR currentDirectory,
	LPCWSTR commandLineArgs [],
	int argsCount,
	LPCWSTR libEnvVariable);

// Base class for all possible responses to a request.
// The ResponseType enum should list all possible response types
// and ReadResponse creates the appropriate response subclass based
// on the response type sent by the client.
// The format of a response is:
//
// Field Name       Field Type          Size (bytes)
// responseLength   int (positive)      4  
// responseType     enum ResponseType   4
// responseBody     Response sublcass   variable
class Response
{
public:
	const enum ResponseType
	{
        MISMATCHED_VERSION,
        COMPLETED
	};

	virtual ResponseType GetResponseType() = 0;
};

// Holds the response from the server
class CompletedResponse : public Response
{
public:
	int exitCode;
	wstring output;
	wstring errorOutput;

	virtual ResponseType GetResponseType() { return COMPLETED; }
	CompletedResponse() = default;
	CompletedResponse(CompletedResponse&& other);
	CompletedResponse(int, wstring&&, wstring&&);

	CompletedResponse& operator=(CompletedResponse&& other);
};

CompletedResponse ReadResponse(IPipe&);