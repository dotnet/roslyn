#include "stdafx.h"
#include "pipe_utils.h"
#include "protocol.h"
#include "logging.h"
#include "UIStrings.h"

using namespace std;

Request::Request(Request&& other)
    : ProtocolVersion(other.ProtocolVersion)
    , Language(other.Language)
    , arguments(other.arguments)
{ }

// We use a request buffer to format an entire request. We do this because its more efficient,
// and also we need to know the full size of the request first.
Request::Request(
    int version,
    RequestLanguage language,
    vector<Argument>&& arguments)
    : ProtocolVersion(version)
    , Language(language)
{
    swap(this->arguments, arguments);
}

Request& Request::operator=(Request&& other)
{
    if (this != &other)
    {
        ProtocolVersion = other.ProtocolVersion;
        Language = other.Language;
        arguments = other.arguments;
    }
    return *this;
}

Request::Request(
    RequestLanguage language,
    const wstring&& currentDirectory)
#pragma warning(suppress: 6011)
    : Request(PROTOCOL_VERSION,
              language,
              { Request::Argument(
                  ArgumentId::CURRENTDIRECTORY, 0, move(currentDirectory)) })
{ }

vector<Request::Argument>& Request::Arguments()
{
    return this->arguments;
}

void Request::AddCommandLineArguments(_In_ const list<wstring>& commandLineArgs)
{
    int i;
    list<wstring>::const_iterator iter;
    for (iter = commandLineArgs.cbegin(), i = 0;
         iter != commandLineArgs.cend();
         ++iter, ++i)
    {
        this->arguments.emplace_back(
            ArgumentId::COMMANDLINEARGUMENT, i, wstring(*iter));
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

namespace {
    // Returns size of serialized arguments in bytes
    inline size_t ArgumentsSize(const vector<Argument>& args) {
        size_t res = 0;
        for (const auto& arg : args)
        {
            res += sizeof(int) * 3 // argumentId + argumentIndex + string_size
                + arg.value.size() // string data size with terminating `\0` character
            ;
        }
        
        return res;
    }
} // anonymous namespace

bool Request::WriteToPipe(IPipe& pipe)
{
    vector<BYTE> buffer;
    
    // Reserving buffer to avoid reallocations and data copying
    buffer.reserve(
        sizeof(int) * 3 // ProtocolVersion + Language + arguments.size()
        + ArgumentsSize(this->arguments)
    );

    AddInt32(buffer, this->ProtocolVersion);
    AddInt32(buffer, this->Language);
    
    AddInt32(buffer, static_cast<int>(this->arguments.size()));
    for (auto arg : this->arguments)
    {
        AddArgument(buffer, arg.id, arg.index, arg.value.c_str());
    }

    auto currentSize = static_cast<unsigned int>(buffer.size());
    auto bufferData = buffer.data();
    LogFormatted(IDS_WritingRequest, currentSize);
    return pipe.Write(&currentSize, sizeof(currentSize))
           && pipe.Write(bufferData, currentSize);
}

CompletedResponse::CompletedResponse(CompletedResponse&& other)
    : ExitCode(other.ExitCode)
    , Utf8Output(other.Utf8Output)
    , Output(move(other.Output))
    , ErrorOutput(move(other.ErrorOutput))
{ }

// Encapsulate the response we got from the server.
// output and errorOutput are now owned by the CompletedResponse
CompletedResponse::CompletedResponse(
    int exitCode,
    bool utf8Output,
    wstring&& output,
    wstring&& errorOutput)
    : ExitCode(exitCode)
    , Utf8Output(utf8Output)
    , Output(move(output))
    , ErrorOutput(move(errorOutput))
{ }

CompletedResponse& CompletedResponse::operator=(CompletedResponse&& other)
{
    swap(ExitCode, other.ExitCode);
    swap(Utf8Output, other.Utf8Output);
    swap(this->Output, other.Output);
    swap(this->ErrorOutput, other.Output);
    
    return *this;
}

wstring ReadStringFromPipe(IPipe& pipe)
{
    int stringLength;
    if (!pipe.Read(&stringLength, sizeof(stringLength)))
    {
        FailFormatted(IDS_PipeReadFailed);
    }

    LogFormatted(IDS_StringLength, stringLength);

    wstring string;
    string.resize(stringLength);

    if (!pipe.Read(&string[0], stringLength * sizeof(wchar_t)))
    {
        FailFormatted(IDS_PipeReadFailed);
    }

    return string;
}

bool ReadCompletedResponse(_In_ IPipe& pipe, _Out_ CompletedResponse& response)
{
    int exitCode; 
    if (!pipe.Read(&exitCode, sizeof(exitCode)))
    {
        LogFormatted(IDS_PipeReadFailed);
        return false;
    }
    bool utf8output;
    if (!pipe.Read(&utf8output, sizeof(utf8output)))
    {
        LogFormatted(IDS_PipeReadFailed);
        return false;
    }
    auto output = ReadStringFromPipe(pipe);
    auto errorOutput = ReadStringFromPipe(pipe);

    response = CompletedResponse(exitCode, utf8output, move(output), move(errorOutput));
    return true;
}

// Reads a response from the pipe. If an unexpected response is
// received, throws a FatalError exception.
bool ReadResponse(_In_ IPipe& pipe, _Out_ CompletedResponse& response)
{
    Log(IDS_ReadingResponse);

    int sizeInBytes;
    Response::ResponseType responseType;

    if (!pipe.Read(&sizeInBytes, sizeof(sizeInBytes)))
    {
        LogFormatted(IDS_PipeReadFailed);
        return true;
    }
    LogFormatted(IDS_ResponseSize, sizeInBytes);

    if (!pipe.Read(&responseType, sizeof(responseType)))
    {
        LogFormatted(IDS_PipeReadFailed);
        return true;
    }
    LogFormatted(IDS_ResponseType, responseType);

    switch (responseType)
    {
    case Response::MISMATCHED_VERSION:
        FailWithGetLastError(IDS_VersionMismatch);
        break;
    case Response::COMPLETED:
        break;
    default:
        FailWithGetLastError(IDS_UnknownResponse);
        break;
    }
    return ReadCompletedResponse(pipe, response);
}
