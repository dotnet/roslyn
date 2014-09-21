// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  
// Licensed under the Apache License, Version 2.0.  
// See License.txt in the project root for license information.

#pragma once

#include <memory>
#include <vector>
#include <list>
#include <string>
#include "pipe_utils.h"

using namespace std;

// The version of the protocol
const int PROTOCOL_VERSION = 1;

// The id numbers below are just random. It's useful to use id numbers
// that won't occur accidentally for debugging.
enum RequestLanguage {
	// csc -- compile C#
	CSHARPCOMPILE = 0x44532521,
	// vbc -- compiler VB
	VBCOMPILE = 0x44532522,
};

// Possible arguments to the server or the compilation
enum ArgumentId {
	// The current directory of the client
	CURRENTDIRECTORY = 0x51147221,
	// A comment line argument. The argument index indicates which one (0 .. N)
	COMMANDLINEARGUMENT,
	// The "LIB" environment variable of the client
	LIBENVVARIABLE,
	// How long to extend compiler server lifetime
	KEEPALIVE
};

enum KeepAlive {
	DEFAULT = -2,
	FOREVER = -1,
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
	int ProtocolVersion;
	RequestLanguage Language;

	struct Argument {
		ArgumentId id;
		int index;
		wstring value;

		Argument(ArgumentId id, int index, wstring&& value)
			: id(id), index(index), value(value)
		{}

		bool operator==(const Request::Argument& right) const
		{
			return this->id == right.id
				&& this->index == right.index
				&& this->value == right.value;
		}
	};

	Request(Request&& other);
	Request(int version,
		    RequestLanguage language,
			vector<Argument>&& arguments);
	Request(RequestLanguage,
			wstring&& currentDirectory);

	Request& operator=(Request&& other);

	vector<Argument>& Arguments();

	void AddCommandLineArguments(list<wstring>& commandLineArgs);
	void AddLibEnvVariable(wstring&& value);
	void AddKeepAlive(wstring&& keepAlive);

	// Write the request buffer to the pipe, prefixed by its length.
    // This procedure either succeeds or logs an error and exits the process.
	bool WriteToPipe(IPipe&);

private:
	vector<Argument> arguments;
};

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
	int ExitCode;
	bool Utf8Output;
	wstring Output;
	wstring ErrorOutput;

	virtual ResponseType GetResponseType() { return COMPLETED; }
	CompletedResponse() = default;
	CompletedResponse(CompletedResponse&& other);
	CompletedResponse(int, bool, wstring&&, wstring&&);

	CompletedResponse& operator=(CompletedResponse&& other);
};

CompletedResponse ReadResponse(IPipe&);