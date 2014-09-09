#define CATCH_CONFIG_MAIN
#include "catch.hpp"
#include "pipe_extensions.h"

TEST_CASE("Simple request without utf-8") {
	auto language = REQUESTID_CSHARPCOMPILE;
	TCHAR currentDirectory[] = L"";
	LPCWSTR args[] = {
		L"test.cs"
	};
	int count = 1;

	auto request = CreateRequest(
		language,
		currentDirectory,
		args,
		count,
		nullptr);

	REQUIRE(PROTOCOL_VERSION == request.protocolVersion);
	REQUIRE(language == request.language);
	REQUIRE_FALSE(request.utf8Output);

	vector<Request::Argument> expectedArgs = {
		Request::Argument(ARGUMENTID_CURRENTDIRECTORY, 0, L""),
		Request::Argument(ARGUMENTID_COMMANDLINEARGUMENT, 0, L"test.cs"),
	};

	REQUIRE(request.arguments == expectedArgs);

	vector<byte> expectedBytes = {
		0x32, 0x0, 0x0, 0x0, // Size of request
		0x1, 0x0, 0x0, 0x0,  // Protocol version
		0x21, 0x25, 0x53, 0x44, // C# compile token
		0x2, 0x0, 0x0, 0x0, // Number of arguments
		0x21, 0x72, 0x14, 0x51, // Current directory token
		0x0, 0x0, 0x0, 0x0, // Index
		0x0, 0x0, 0x0, 0x0, // Length of value string
		0x22, 0x72, 0x14, 0x51, // Command line arg token
		0x0, 0x0, 0x0, 0x0, // Index
		0x7, 0x0, 0x0, 0x0, // Length of value string in characters
		0x74, 0x0, 0x65, 0x0, 0x73, // 't', 'e', 's'
		0x0, 0x74, 0x0, 0x2e, 0x0, // 't', '.'
		0x63, 0x0, 0x73, 0x0 // 'c', 's'
	};


	WriteOnlyMemoryPipe pipe;
	REQUIRE(request.WriteToPipe(pipe));

	REQUIRE(pipe.Bytes() == expectedBytes);
}

TEST_CASE("Simple request with UTF-8") {
	auto language = REQUESTID_CSHARPCOMPILE;
	TCHAR currentDirectory[] = L"";
	LPCWSTR args[] = {
		L"/utf8output",
		L"test.cs"
	};
	int count = 2;

	auto request = CreateRequest(
		language,
		currentDirectory,
		args,
		count,
		nullptr);

	REQUIRE(PROTOCOL_VERSION == request.protocolVersion);
	REQUIRE(language == request.language);
	REQUIRE(request.utf8Output);

	vector<Request::Argument> expectedArgs = {
		Request::Argument(ARGUMENTID_CURRENTDIRECTORY, 0, L""),
		Request::Argument(ARGUMENTID_COMMANDLINEARGUMENT, 0, L"/utf8output"),
		Request::Argument(ARGUMENTID_COMMANDLINEARGUMENT, 1, L"test.cs"),
	};

	REQUIRE(request.arguments == expectedArgs);

	vector<byte> expectedBytes = {
		0x54, 0x0, 0x0, 0x0, // Size of request
		0x1, 0x0, 0x0, 0x0,  // Protocol version
		0x21, 0x25, 0x53, 0x44, // C# compile token
		0x3, 0x0, 0x0, 0x0, // Number of arguments
		0x21, 0x72, 0x14, 0x51, // Current directory token
		0x0, 0x0, 0x0, 0x0, // Index
		0x0, 0x0, 0x0, 0x0, // Length of value string
		0x22, 0x72, 0x14, 0x51, // Command line arg token
		0x0, 0x0, 0x0, 0x0, // Index
		0xb, 0x0, 0x0, 0x0, // Length of value string in characters
		0x2f, 0x0, 0x75, 0x0, // '/', 'u'
		0x74, 0x0, 0x66, 0x0, // 't', 'f'
		0x38, 0x0, 0x6f, 0x0, // '8, 'o'
		0x75, 0x0, 0x74, 0x0, // 'u', 't'
		0x70, 0x0, 0x75, 0x0, // 'p', 'u'
		0x74, 0x0, // 't'
		0x22, 0x72, 0x14, 0x51, // Command line arg token
		0x1, 0x0, 0x0, 0x0, // Index
		0x7, 0x0, 0x0, 0x0, // Length of value string in characters
		0x74, 0x0, 0x65, 0x0, 0x73, // 't', 'e', 's'
		0x0, 0x74, 0x0, 0x2e, 0x0, // 't', '.'
		0x63, 0x0, 0x73, 0x0 // 'c', 's'
	};

	WriteOnlyMemoryPipe pipe;
	request.WriteToPipe(pipe);

	REQUIRE(pipe.Bytes() == expectedBytes);
}