// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#include "stdafx.h"
#include <memory>
#include <algorithm>
#include "logging.h"
#include "native_client.h"
#include "pipe_utils.h"
#include "smart_resources.h"

// This is small, native code executable which opens a named pipe 
// to the compiler server to do the actual compilation. It is a native code
// executable because the entire point is to start fast, and then use the 
// "hot" compiler server to do compilation. This is a big win because normally
// you are doing many compilation in a row. For a single compilation, the additional
// overhead of starting this small native exe is small.
//
// This code searches for an existing SERVERNAME (in the same directory
// as this executable), and connects to it. If that fails, it starts a new one
// and connects to it.
//
// It then sends the command line arguments to that process, and gets back the
// return code, stdout, and stderr. It prints those out and exits.

// The name of the server.
const wchar_t * const SERVERNAME = L"VBCSCompiler.exe";

// The name of the named pipe. A process id is appended to the end.
const wchar_t * const PIPENAME = L"VBCSCompiler";

wstring GetCurrentDirectory()
{
    int sizeNeeded = GetCurrentDirectory(0, nullptr);
    if (0 == sizeNeeded) 
    {
        FailWithGetLastError(L"GetCurrentDirectory failed");
    }

    wstring result;
    result.resize(sizeNeeded);

    auto written = (int)GetCurrentDirectory(sizeNeeded, &result[0]);
    if (written == 0 || written > sizeNeeded)
    {
        FailWithGetLastError(L"GetCurrentDirectory failed");
    }

	result.resize(written);
    return result;
}

// Returns the arguments passed to the executable (including
// the executable name)
std::unique_ptr<LPCWSTR, decltype(&::LocalFree)> GetCommandLineArgs(int &argsCount)
{
    auto args = const_cast<LPCWSTR*>(CommandLineToArgvW(GetCommandLine(), &argsCount));
    if (args == nullptr) 
    {
        FailWithGetLastError(L"CommandLineToArgvW failed");
    }
    return unique_ptr<LPCWSTR, decltype(&::LocalFree)>(args, ::LocalFree);
}

const DWORD MinConnectionAttempts = 3;        // Always make at least three attempts (matters when each attempt takes a long time (under load)).
const DWORD TimeOutMsExistingProcess = 2000;  // Spend up to 2s connecting to existing process (existing processes should be always responsive).
const DWORD TimeOutMsNewProcess = 60000;      // Spend up to 60s connection to new process, to allow time for it to start.

// Is the give FILE* a console? Stolen from native compiler.
bool IsConsole(FILE *fd)
{
    int fh = _fileno(fd);

    HANDLE hFile = (HANDLE)_get_osfhandle(fh);

    DWORD dwType = GetFileType(hFile);

    dwType &= ~FILE_TYPE_REMOTE;

    if (dwType != FILE_TYPE_CHAR)
    {
        return false;
    }

    DWORD dwMode;

    return GetConsoleMode(hFile, &dwMode) != 0;
}

// Output a unicode string, taking into account console code pages 
// and possible /utf8output options.
void OutputWideString(FILE * outputFile, wstring str, bool utf8Output)
{
    UINT cp;

    if (!IsConsole(outputFile) && utf8Output)
    {
        cp = CP_UTF8;
    }
    else 
    {
        cp = GetConsoleOutputCP();
    }

    int strLength = (int)str.length();

    auto bytesNeeded = WideCharToMultiByte(cp, 0, str.c_str(), strLength, NULL, 0, NULL, NULL);
    auto outputBuffer = make_unique<char[]>(bytesNeeded);
    WideCharToMultiByte(cp, 0, str.c_str(), strLength, outputBuffer.get(), bytesNeeded, NULL, NULL);
    fwrite(outputBuffer.get(), 1, bytesNeeded, outputFile);
}

// Output the response we got back from the server onto our stdout and stderr.
void OutputResponse(const CompletedResponse &response, bool utf8Output)
{
    OutputWideString(stdout, response.output, utf8Output);
    OutputWideString(stderr, response.errorOutput, utf8Output);
}

// Get the expected process path of the server. We assume that the server EXE
// will be in the same directory as the client EXE. This allows us to support
// side-by-side install of different compilers. We only connect to servers that
// have the expected full process path.
bool GetExpectedProcessPath(LPWSTR szProcessName, int cch)
{
    if (GetModuleFileNameW(NULL, szProcessName, cch) != 0)
    {
        LPWSTR lastBackslash = wcsrchr(szProcessName, '\\');
        if (lastBackslash != NULL) 
        {
            *(lastBackslash + 1) = '\0';
            return SUCCEEDED(StringCchCatW(szProcessName, cch, SERVERNAME));
        }
    }

    return false;
}

// Try to connect to a named pipe on the given process id.
HANDLE ConnectToProcess(DWORD processID, int timeoutMs)
{
    // Machine-local named pipes are named "\\.\pipe\<pipename>".
    // We use the pipe name followed by the process id.

    TCHAR szPipeName[MAX_PATH];
    StringCchPrintf(szPipeName, MAX_PATH, L"\\\\.\\pipe\\%ws%d", PIPENAME, processID);

    // Open the pipe.
    HANDLE pipeHandle = OpenPipe(szPipeName, timeoutMs);
    if (pipeHandle != INVALID_HANDLE_VALUE)
    {
        Log(L"Sucessfully opened pipe");
        return pipeHandle;
    }

    Log(L"Failed to open pipe - can try another server process.");
    return NULL;
}

// Perform the compilation. If the compilation completes, this function
// never returns, exiting with the exit code of the compilation.
bool TryCompile(HANDLE pipeHandle,
                RequestLanguage language,
                LPCWSTR currentDirectory,
                LPCWSTR commandLineArgs[],
                int argsCount,
                LPCWSTR libEnvVariable,
                bool &utf8Output,
                CompletedResponse &response)
{
    auto request = CreateRequest(language,
                      currentDirectory,
                      commandLineArgs,
                      argsCount,
                      libEnvVariable);

    utf8Output = request.utf8Output;

    RealPipe wrapper(pipeHandle);
    if (!request.WriteToPipe(wrapper)) 
    {
        Log(L"Failed to write request - can try another server process.");
        return false;
    }

    Log(L"Successfully wrote request.");

    // We should expect a completed response since
    // the only other option is a an erroroneous response
    // which will generate an exception.
    response = ReadResponse(wrapper);
    Log(L"Successfully read response.");

    // We got a response.
    return true;
}

// Get the process ids of all processes on the system.
bool GetAllProcessIds(vector<DWORD> &processes)
{
    Log(L"Enumerating all process IDs");

    processes.resize(64);
    DWORD bytesWritten;

    for (;;)
    {
        if (EnumProcesses(processes.data(),
                          static_cast<int>(processes.size()) * sizeof(DWORD),
                          &bytesWritten))
        {
            int writtenDwords = bytesWritten / sizeof(DWORD);
            if (writtenDwords != processes.size())
            {
                processes.resize(writtenDwords);
                return true;
            }
            else
            {
                processes.resize(writtenDwords * 2);
            }
        }
        else
        {
            LogWin32Error(L"EnumProcesses");
            return false;
        }
    }
}

// For devdiv we need to set up a 64-bit CLR which we do by setting the 
// appropriate environment variables and letting our environment be
// inherited by the server. The variables are as follows:
//  COMPLUS_InstallRoot=$(RazzleToolPath)\tools\amd64\managed
//  COMPLUS_Version=v4.5
// We only setup the environment if the $RazzleToolPath variable is set.
void SetupDevDivEnvironment()
{
    // Constants in the environment
    auto suffix = L"\\amd64\\managed";
    auto installRoot = L"COMPLUS_InstallRoot";
    auto toolPath = L"RazzleToolPath";

    wstring buffer;
    if (!GetEnvVar(toolPath, buffer))
    {
        return;
    }
    buffer += suffix;

    if (!SetEnvironmentVariable(installRoot, buffer.c_str()))
        FailWithGetLastError(L"SetEnvironmentVariable install root");
    if (!SetEnvironmentVariable(L"COMPLUS_Version", L"v4.5")) 
        FailWithGetLastError(L"SetEnvironmentVariable version");
}

// Start a new server process with the given executable name,
// and return the process id of the process. On error, return
// zero.
DWORD CreateNewServerProcess(LPCWSTR executablePath)
{
    STARTUPINFO startupInfo;
    PROCESS_INFORMATION processInfo;
    BOOL success;

    LogFormatted(L"Attempting to create process '%ws'", executablePath);

    memset(&startupInfo, 0, sizeof(startupInfo));
    startupInfo.cb = sizeof(startupInfo);

    // Give the process no standard IO streams.
    startupInfo.dwFlags = STARTF_USESTDHANDLES;
    startupInfo.hStdError = INVALID_HANDLE_VALUE;
    startupInfo.hStdInput = INVALID_HANDLE_VALUE;
    startupInfo.hStdOutput = INVALID_HANDLE_VALUE;

    // If this is devdiv we need to set up the devdiv environment.
    // If this is not devdiv, no environment variables will be changed
    SetupDevDivEnvironment();

	auto driveLetter = make_unique<wchar_t[]>(_MAX_DRIVE);
	auto dirWithoutDrive = make_unique<wchar_t[]>(_MAX_DIR);

	errno_t err;
	if ((err = _wsplitpath_s(executablePath,
						     driveLetter.get(), _MAX_DRIVE,
						     dirWithoutDrive.get(), _MAX_DIR,
						     nullptr, 0,
						     nullptr, 0)))
	{
		FailFormatted(L"Couldn't split the process executable path: %d", err);
	}

	auto dir = wstring(driveLetter.get());
	dir.append(dirWithoutDrive.get());
	auto dirCstr = dir.c_str();

	LogFormatted(L"Creating process with directory %ws", dirCstr);

    success = CreateProcess(executablePath, 
        NULL, // command line,
        NULL, // process attributes
        NULL, // thread attributes
        FALSE, // don't inherit handles
        NORMAL_PRIORITY_CLASS | CREATE_NO_WINDOW | CREATE_UNICODE_ENVIRONMENT,
        NULL, // Inherit environment
        dirCstr, // The process should run in the directory the executable is located
        &startupInfo,
        &processInfo);

    if (success) 
    {
        // We don't need the process and thread handles.
        LogFormatted(L"Successfully created process with process id %d", processInfo.dwProcessId);
        CloseHandle(processInfo.hProcess);
        CloseHandle(processInfo.hThread);
        return processInfo.dwProcessId;
    }
    else 
    {
        LogWin32Error(L"Creating process");
        return 0;
    }
}

// Get the full name of a process.
bool ProcessHasSameName(HANDLE processHandle, LPCWSTR expectedName)
{
    // Get the process name.
	WCHAR buffer[MAX_PATH] = {};
    DWORD length = _countof(buffer);
#pragma warning(suppress: 6386)
    return QueryFullProcessImageNameW(processHandle, 
		                              0, /*dwFlags: 0 = Win32 format */
									  buffer,
									  &length)
        && lstrcmpiW(buffer, expectedName) == 0;
}

BOOL GetTokenUserAndElevation(HANDLE tokenHandle,
                              unique_ptr<TOKEN_USER> &userInfo,
                              unique_ptr<TOKEN_ELEVATION> &userElevation)
{
    DWORD requiredLength;
    GetTokenInformation(tokenHandle, TokenUser, NULL, 0, &requiredLength);
    userInfo.reset((PTOKEN_USER)::operator new(requiredLength));
    if (!GetTokenInformation(tokenHandle, TokenUser, userInfo.get(), requiredLength, &requiredLength))
    {
        return false;
    }

    GetTokenInformation(tokenHandle, TokenElevation, NULL, 0, &requiredLength);
    userElevation.reset((PTOKEN_ELEVATION)::operator new(requiredLength));
    return GetTokenInformation(tokenHandle, TokenElevation, userElevation.get(), requiredLength, &requiredLength);
}

bool ProcessHasSameUserAndElevation(HANDLE processHandle,
                                    TOKEN_USER const * firstInfo,
                                    TOKEN_ELEVATION const * firstElevation)
{
    HANDLE tokenHandle;
    if (OpenProcessToken(processHandle, TOKEN_QUERY, &tokenHandle))
    {
        unique_ptr<TOKEN_USER> otherInfo;
        unique_ptr<TOKEN_ELEVATION> otherElevation;
        DWORD requiredLength;

        GetTokenInformation(tokenHandle, TokenUser, NULL, 0, &requiredLength);
        otherInfo.reset((PTOKEN_USER)::operator new(requiredLength));

        GetTokenInformation(tokenHandle, TokenElevation, NULL, 0, &requiredLength);
        otherElevation.reset((PTOKEN_ELEVATION)::operator new(requiredLength));

        if (GetTokenUserAndElevation(tokenHandle, otherInfo, otherElevation)
            && EqualSid(otherInfo->User.Sid, firstInfo->User.Sid)
            && otherElevation->TokenIsElevated == firstElevation->TokenIsElevated)
        {
            return true;
        }
    }
    return false;
}

HANDLE TryExistingProcesses(LPCWSTR expectedProcessName)
{
    unique_ptr<TOKEN_USER> userInfo;
    unique_ptr<TOKEN_ELEVATION> elevationInfo;

    HANDLE tempHandle;
    if (!OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY, &tempHandle))
    {
        FailWithGetLastError(L"Couldn't get current process token:");
    }

    SmartHandle tokenHandle(tempHandle);
    if (!GetTokenUserAndElevation(tokenHandle.get(), userInfo, elevationInfo))
    {
        FailWithGetLastError(L"Couldn't get user token information:");
    }

    vector<DWORD> processes;
    if (GetAllProcessIds(processes))
    {
        LogFormatted(L"Found %d processes", processes.size());

        // Check each process to find one with the right name and user
        for (auto processId : processes)
        {
            if (processId != 0)
            {
                auto processHandle = SmartHandle(OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, FALSE, processId));

                if (processHandle.get() != nullptr
                    // Check if the process has the same name
                    && ProcessHasSameName(processHandle.get(), expectedProcessName)
                    // Check if the process is owned by the same user
                    && ProcessHasSameUserAndElevation(processHandle.get(), userInfo.get(), elevationInfo.get()))
                {
                    LogFormatted(L"Found process with id %d", processId);
                    HANDLE pipeHandle = ConnectToProcess(processId, TimeOutMsExistingProcess);
                    if (pipeHandle != NULL)
                    {
                        return pipeHandle;
                    }
                }
            }
        }
    }
    return NULL;
}

CompletedResponse Run(
    RequestLanguage language,
    LPCWSTR currentDirectory,
    LPCWSTR * commandLineArgs,
    int argsCount,
    LPCWSTR libEnvVar,
    bool &utf8Output)
{
    wchar_t expectedProcessPath[MAX_PATH];

    InitializeLogging();

    LogTime();

    if (!GetExpectedProcessPath(expectedProcessPath, MAX_PATH))
    {
        FailWithGetLastError(L"GetExpectedProcessPath failed");
    }

    // First attempt to grab the mutex
    wstring mutexName(expectedProcessPath);
	replace(mutexName.begin(), mutexName.end(), L'\\', L'/');

    Log(L"Creating mutex.");

    SmartMutex createProcessMutex(mutexName.c_str());

    // If the mutex already exists and someone else has it, we should wait
    if (!createProcessMutex.HoldsMutex())
    {
        createProcessMutex.Wait(TimeOutMsNewProcess);
    }

    SmartHandle pipeHandle = nullptr;
    DWORD processId = 0;

    // Proceed with the mutex
    if (createProcessMutex.HoldsMutex())
    {
        // Check for already running processes in case someone came in before us
        Log(L"Trying existing processes.");
        pipeHandle.reset(TryExistingProcesses(expectedProcessPath));
        if (pipeHandle != nullptr)
        {
            Log(L"Connected, releasing mutex.");
            createProcessMutex.release();
            Log(L"Compiling.");

            CompletedResponse response;
            if (TryCompile(pipeHandle.get(),
                           language,
                           currentDirectory,
                           commandLineArgs,
                           argsCount,
                           libEnvVar,
                           utf8Output,
                           response))
            {
                return response;
            }

            Log(L"Compilation failed with existing process, retrying once.");
        }
        else
        {
            Log(L"No success with existing processes - try creating a new one.");
            processId = CreateNewServerProcess(expectedProcessPath);
            if (processId != 0)
            {
                LogFormatted(L"Connecting to newly created process id %d", processId);
                pipeHandle.reset(ConnectToProcess(processId, TimeOutMsNewProcess));
                if (pipeHandle != nullptr)
                {
                    // Let everyone else access our process
                    Log(L"Connected, releasing mutex.");
                    createProcessMutex.release();
                    Log(L"Compiling.");
                    CompletedResponse response;
                    if (TryCompile(pipeHandle.get(),
                                   language,
                                   currentDirectory,
                                   commandLineArgs,
                                   argsCount,
                                   libEnvVar,
                                   utf8Output,
                                   response))
                    {
                        return response;
                    }
                }
            }

            Log(L"No success with created process, retrying once.");
        }

        createProcessMutex.release();

        // Sleep shortly before retying in case the failure was due to
        // resource contention
        Sleep(500);
    }

    // Try one time without a mutex
    Log(L"Trying without mutex");
    processId = CreateNewServerProcess(expectedProcessPath);
    if (processId != 0)
    {
        LogFormatted(L"Connecting to newly created process id %d", processId);
        pipeHandle.reset(ConnectToProcess(processId, TimeOutMsNewProcess));
        if (pipeHandle != nullptr)
        {
            // Let everyone else access our process
            Log(L"Connected to new process.");
            CompletedResponse response;
            if (TryCompile(pipeHandle.get(),
                           language,
                           currentDirectory,
                           commandLineArgs,
                           argsCount,
                           libEnvVar,
                           utf8Output,
                           response))
            {
                return response;
            }
        }
    }

    // We're about to exit due to an error above. Check to see if the
    // server has crashed or disconnected. If so, print a better error
    // message.

    // If the pipe handle is null, it's likely we never even connected to the
    // pipe
    if (pipeHandle == nullptr)
    {
        FailFormatted(L"Could not connect to server pipe");
    }
    else if (processId != 0)
    {
        SmartHandle process(OpenProcess(PROCESS_QUERY_INFORMATION, FALSE, processId));
        if (process == NULL)
        {
            FailFormatted(L"Could not find server process -- compiler may have disconnected or crashed due to error.");
        }
        else
        {
            DWORD exitCode;
            if (GetExitCodeProcess(process.get(), &exitCode))
            {
                FailFormatted(L"Server process has crashed with error code: %d\n", exitCode);
            }
        }
    }
    else
    {
        FailWithGetLastError(L"Unknown failure");
    }

    // Unreachable
	return CompletedResponse();
}

int Run(RequestLanguage language)
{
    try
    {
        auto currentDirectory = GetCurrentDirectory();
        int argsCount;
        auto commandLineArgs = GetCommandLineArgs(argsCount);
        wstring libEnvVariable;
        bool utf8Output;

        // Change stderr, stdout to binary, because the output we get from the server already 
        // has CR and LF in it. If we don't do this, we get CR CR LF at each newline.
        (void)_setmode(_fileno(stdout), _O_BINARY);
        (void)_setmode(_fileno(stderr), _O_BINARY);

        auto response = Run(
            language,
            currentDirectory.c_str(),
            // Don't include the name of the process
            commandLineArgs.get() + 1,
            argsCount - 1,
            GetEnvVar(L"LIB", libEnvVariable) ? libEnvVariable.c_str() : nullptr,
            utf8Output);

        OutputResponse(response, utf8Output);
        return response.exitCode;
    }
    catch (FatalError &e)
    {
        OutputWideString(stderr, e.message, true);
    }
    return 1;
}