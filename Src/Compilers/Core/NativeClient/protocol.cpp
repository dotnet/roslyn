#include "stdafx.h"
#include "pipe_utils.h"
#include "protocol.h"
#include "logging.h"

using namespace std;

Request::Request(Request&& other)
	: ProtocolVersion(other.ProtocolVersion)
	, Language(other.Language)
	, Utf8Output(other.Utf8Output)
	, arguments(other.arguments)
{ }

// We use a request buffer to format an entire request. We do this because its more efficient,
// and also we need to know the full size of the request first.
Request::Request(
    int version,
    RequestLanguage language,
    bool utf8Output,
    vector<Argument>&& arguments)
	: ProtocolVersion(version),
	  Language(language),
	  Utf8Output(utf8Output)
{
	swap(this->arguments, arguments);
}

Request& Request::operator=(Request&& other)
{
	if (this != &other)
	{
		ProtocolVersion = other.ProtocolVersion;
		Language = other.Language;
		Utf8Output = other.Utf8Output;
		arguments = other.arguments;
	}
	return *this;
}

Request::Request(
	RequestLanguage language,
	wstring&& currentDirectory)
	: Request(PROTOCOL_VERSION,
			  language,
		      false,
		      { Request::Argument(
				  ArgumentId::CURRENTDIRECTORY, 0, move(currentDirectory)) })
{ }

vector<Request::Argument>& Request::Arguments()
{
	return this->arguments;
}

void Request::AddCommandLineArguments(vector<wstring>& commandLineArgs)
{
    for (size_t i = 0; i < commandLineArgs.size(); ++i)
    {
		this->arguments.emplace_back(ArgumentId::COMMANDLINEARGUMENT, i,
			wstring(commandLineArgs.at(i)));
    }
}

void Request::AddLibEnvVariable(wstring&& value)
{
	arguments.emplace_back(ArgumentId::LIBENVVARIABLE, 0, move(value));
}

void Request::AddKeepAlive(wstring&& value)
{
	arguments.emplace_back(ArgumentId::KEEPALIVE, 0, move(value));
}

// TODO(angocke): This function is dependent on the machine architecture being little
// endian. We should evaluate other serialization options.
void AddData(vector<BYTE> &buffer, LPCVOID pData, size_t cData)
{
    auto byteView = static_cast<const BYTE*>(pData);
    buffer.insert(buffer.end(), byteView, byteView + cData);
}

void AddInt32(vector<BYTE> &buffer, int data)
{
    AddData(buffer, &data, sizeof(data));
}

void AddString(vector<BYTE> &buffer, LPCWSTR str)
{
    // Length without null terminator
    auto cch = (int)wcslen(str);

    AddInt32(buffer, cch);
    AddData(buffer, str, cch * sizeof(WCHAR));
}

void AddArgument(vector<BYTE> &buffer, int argumentId, int argumentIndex, LPCWSTR value)
{
    AddInt32(buffer, argumentId);
    AddInt32(buffer, argumentIndex);
    AddString(buffer, value);
}

bool Request::WriteToPipe(IPipe& pipe)
{
    vector<BYTE> buffer;

    AddInt32(buffer, this->ProtocolVersion);
    AddInt32(buffer, this->Language);
    
    AddInt32(buffer, static_cast<int>(this->arguments.size()));
    for (auto arg : this->arguments)
    {
        AddArgument(buffer, arg.id, arg.index, arg.value.c_str());
    }

    auto currentSize = static_cast<unsigned int>(buffer.size());
    auto bufferData = buffer.data();
    LogFormatted(L"Writing request of size %u", currentSize);
    return pipe.Write(&currentSize, sizeof(currentSize))
           && pipe.Write(bufferData, currentSize);
}

CompletedResponse::CompletedResponse(CompletedResponse&& other)
	: exitCode(other.exitCode)
	, output(move(other.output))
	, errorOutput(move(other.errorOutput))
{ }

// Encapsulate the response we got from the server.
// output and errorOutput are now owned by the CompletedResponse
CompletedResponse::CompletedResponse(int exitCode,
                                     wstring&& output,
                                     wstring&& errorOutput)
{
    this->exitCode = exitCode;
	swap(this->output, output);
	swap(this->errorOutput, errorOutput);
}

CompletedResponse& CompletedResponse::operator=(CompletedResponse&& other)
{
	if (this != &other)
	{
		exitCode = other.exitCode;
		swap(output, other.output);
		swap(errorOutput, other.errorOutput);
	}
	return *this;
}

wstring ReadStringFromPipe(IPipe& pipe)
{
    int stringLength;
    if (!pipe.Read(&stringLength, sizeof(stringLength)))
    {
        FailFormatted(L"Pipe read failed\n");
    }

    LogFormatted(L"String length = %d", stringLength);

	wstring string;
	string.resize(stringLength);

    if (!pipe.Read(&string[0], stringLength * sizeof(wchar_t)))
    {
        FailFormatted(L"Pipe read failed\n");
    }

	return string;
}

CompletedResponse ReadCompletedResponse(IPipe& pipe)
{
    int exitCode; 
    if (!pipe.Read(&exitCode, sizeof(exitCode)))
    {
        FailFormatted(L"Pipe read failed\n");
    }
    auto output = ReadStringFromPipe(pipe);
    auto errorOutput = ReadStringFromPipe(pipe);

    return CompletedResponse(exitCode, move(output), move(errorOutput));
}

// Reads a response from the pipe. If an unexpected response is
// received, throws a FatalError exception.
CompletedResponse ReadResponse(IPipe& pipe)
{
    Log(L"Reading response");

    int sizeInBytes;
    Response::ResponseType responseType;

    if (!pipe.Read(&sizeInBytes, sizeof(sizeInBytes)))
    {
        FailFormatted(L"Pipe read failed\n");
    }
    LogFormatted(L"Response has %d bytes", sizeInBytes);

    if (!pipe.Read(&responseType, sizeof(responseType)))
    {
        FailFormatted(L"Pipe read failed\n");
    }
    LogFormatted(L"Response type: %d", responseType);

    switch (responseType)
    {
    case Response::MISMATCHED_VERSION:
		FailWithGetLastError(L"Received mismatched version response from server. "
			L"Are your client and server binaries out of sync?");
		break;
    case Response::COMPLETED:
		break;
    default:
        FailWithGetLastError(L"Received unknown response from server");
        break;
    }
	return ReadCompletedResponse(pipe);
}