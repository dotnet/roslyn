// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#include "stdafx.h"
#include <memory>
#include <algorithm>
#include <string>
#include "logging.h"
#include "native_client.h"
#include "pipe_utils.h"
#include "smart_resources.h"
#include "satellite.h"
#include "UIStrings.h"

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

// Module to load resources from.
HINSTANCE g_hinstMessages;

wstring GetCurrentDirectory()
{
    int sizeNeeded = GetCurrentDirectory(0, nullptr);
    if (0 == sizeNeeded)
    {
        FailWithGetLastError(IDS_GetCurrentDirectoryFailed);
    }

    wstring result;
    result.resize(sizeNeeded);

    auto written = (int)GetCurrentDirectory(sizeNeeded, &result[0]);
    if (written == 0 || written > sizeNeeded)
    {
        FailWithGetLastError(IDS_GetCurrentDirectoryFailed);
    }

    result.resize(written);
    return result;
}

// Returns the arguments passed to the executable (including
// the executable name)
std::unique_ptr<LPCWSTR, decltype(&::LocalFree)> GetCommandLineArgs(_Out_ int& argsCount)
{
    auto args = const_cast<LPCWSTR*>(CommandLineToArgvW(GetCommandLine(), &argsCount));
    if (args == nullptr)
    {
        FailWithGetLastError(IDS_CommandLineToArgvWFailed);
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
void OutputWideString(_In_ FILE * outputFile, _In_ const wstring str, bool utf8Output)
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
void OutputResponse(_In_ const CompletedResponse& response)
{
    auto utf8output = response.Utf8Output;
    OutputWideString(stdout, response.Output, utf8output);
    OutputWideString(stderr, response.ErrorOutput, utf8output);
}

// Get the expected process path of a compiler EXE. We assume that the EXE
// will be in the same directory as the client EXE. This allows us to support
// side-by-side install of different compilers. We only connect to servers that
// have the expected full process path.
bool GetExpectedProcessPath(
	_In_z_ LPCWSTR processName,
	_Out_ wstring& processPath)
{
	processPath.clear();
	processPath.resize(MAX_PATH);
	while (GetModuleFileNameW(NULL, &processPath[0], processPath.size()) == 0)
	{
		if (GetLastError() == ERROR_INSUFFICIENT_BUFFER) {
			processPath.resize(processPath.size() * 2);
		} else {
			return false;
		}
	}
	// Find the last backslash, which should be the directory of the client
	// EXE, and append the new process name
	auto lastBackslash = processPath.find_last_of('\\');
	if (lastBackslash != string::npos)
	{
		processPath.erase(lastBackslash + 1);
		processPath.append(processName);
		return true;
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
        Log(IDS_SucessfullyOpenedPipe);
        return pipeHandle;
    }

    Log(IDS_FailedToOpenPipe);
    return NULL;
}

/// <summary>
/// Perform the compilation. If the compilation completes, this function
/// never returns, exiting with the exit code of the compilation.
/// </summary>
/// <param name='keepAlive'>
/// Set to the empty string if no keepAlive should be used
/// </param>
_Success_(return != false)
bool TryCompile(HANDLE pipeHandle,
				RequestLanguage language,
				_In_z_ LPCWSTR currentDirectory,
				_In_ const list<wstring>& commandLineArgs,
				_In_opt_z_ LPCWSTR libEnvVariable,
				_In_ const wstring& keepAlive,
				_Out_ CompletedResponse& response)
{
    auto request = Request(language, currentDirectory);
    request.AddCommandLineArguments(commandLineArgs);
    if (libEnvVariable != nullptr) {
        request.AddLibEnvVariable(wstring(libEnvVariable));
    }
    if (!keepAlive.empty()) {
        request.AddKeepAlive(wstring(keepAlive));
    }

    RealPipe wrapper(pipeHandle);
    if (!request.WriteToPipe(wrapper))
    {
        Log(IDS_FailedToWriteRequest);
        return false;
    }

    Log(IDS_SuccessfullyWroteRequest);

    // We should expect a completed response since
    // the only other option is a an erroroneous response
    // which will generate an exception.
	if (ReadResponse(wrapper, response)) {
		Log(IDS_SuccessfullyReadResponse);
		return true;
	}
	return false;
}

// Get the process ids of all processes on the system.
bool GetAllProcessIds(_Out_ vector<DWORD>& processes)
{
    Log(IDS_EnumeratingProcessIDs);

    processes.clear();
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
DWORD CreateNewServerProcess(_In_z_ LPCWSTR executablePath)
{
    STARTUPINFO startupInfo;
    PROCESS_INFORMATION processInfo;
    BOOL success;

    LogFormatted(IDS_AttemptingToCreateProcess, executablePath);

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
        FailFormatted(IDS_SplitProcessPathError, err);
    }

    auto createPath = make_unique<wchar_t[]>(MAX_PATH);
    if ((err = _wmakepath_s(createPath.get(),
        MAX_PATH,
        driveLetter.get(),
        dirWithoutDrive.get(),
        nullptr,
        nullptr)))
    {
        FailFormatted(IDS_MakeNewProcessPathError, err);
    }

    success = CreateProcess(executablePath,
        NULL, // command line,
        NULL, // process attributes
        NULL, // thread attributes
        FALSE, // don't inherit handles
        NORMAL_PRIORITY_CLASS | CREATE_NO_WINDOW | CREATE_UNICODE_ENVIRONMENT,
        NULL, // Inherit environment
        createPath.get(), // The process should run in the directory the executable is located
        &startupInfo,
        &processInfo);

    if (success)
    {
        // We don't need the process and thread handles.
        LogFormatted(IDS_CreatedProcess, processInfo.dwProcessId);
        CloseHandle(processInfo.hProcess);
        CloseHandle(processInfo.hThread);
        return processInfo.dwProcessId;
    }
    else
    {
        LogWin32Error(IDS_CreatingProcess);
        return 0;
    }
}

// Get the full name of a process.
bool ProcessHasSameName(HANDLE processHandle, _In_z_ LPCWSTR expectedName)
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
    _Out_ unique_ptr<TOKEN_USER>& userInfo,
    _Out_ unique_ptr<TOKEN_ELEVATION>& userElevation)
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
    _In_ TOKEN_USER const * firstInfo,
    _In_ TOKEN_ELEVATION const * firstElevation)
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

HANDLE TryExistingProcesses(_In_z_ LPCWSTR expectedProcessName)
{
    unique_ptr<TOKEN_USER> userInfo;
    unique_ptr<TOKEN_ELEVATION> elevationInfo;

    HANDLE tempHandle;
    if (!OpenProcessToken(GetCurrentProcess(), TOKEN_QUERY, &tempHandle))
    {
        FailWithGetLastError(IDS_GetCurrentProcessTokenFailed);
    }

    SmartHandle tokenHandle(tempHandle);
    if (!GetTokenUserAndElevation(tokenHandle.get(), userInfo, elevationInfo))
    {
        FailWithGetLastError(IDS_GetUserTokenFailed);
    }

    vector<DWORD> processes;
    if (GetAllProcessIds(processes))
    {
        LogFormatted(IDS_FoundProcesses, processes.size());

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
                    LogFormatted(IDS_FoundProcess, processId);
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

// N.B. Native client arguments (e.g., /keepalive) are NOT supported in response
// files.
// Aside from separation of concerns, this is important because we endeavor to
// send the exact command line given to the native client to the server, minus
// any native client-specific arguments. If we were to accept native client
// arguments in the response file, we would have to edit the response file to
// remove the argument or mangle the command line given to the server.
void ParseAndValidateClientArguments(
    _Inout_ list<wstring>& arguments,
    _Out_ wstring& keepAliveValue)
{
    keepAliveValue.clear();
    auto iter = arguments.cbegin();
    while (iter != arguments.cend())
    {
        auto arg = *iter;
        if (arg.find(L"/keepalive") == 0)
        {
            auto prefixLen = wcslen(L"/keepalive");

            if (arg.length() < prefixLen + 2 ||
                (arg.at(prefixLen) != L':' && arg.at(prefixLen) != L'='))
            {
                throw FatalError(GetResourceString(IDS_MissingKeepAlive));
            }

            auto value = arg.substr(prefixLen + 1);
            try {
                auto intValue = stoi(value);

                if (intValue < -1) {
                    throw FatalError(GetResourceString(IDS_KeepAliveIsTooSmall));
                }

                keepAliveValue = value;
                iter = arguments.erase(iter);
                continue;
            }
            catch (invalid_argument) {
                throw FatalError(GetResourceString(IDS_KeepAliveIsNotAnInteger));
            }
            catch (out_of_range) {
                throw FatalError(GetResourceString(IDS_KeepAliveIsOutOfRange));
            }
        }

        ++iter;
    }

}

bool TryRunServerCompilation(
    RequestLanguage language,
    _In_z_ LPCWSTR currentDirectory,
	_In_ const list<wstring>& commandLineArgs,
	_In_ const wstring& keepAlive,
    _In_opt_z_ LPCWSTR libEnvVar,
	_Out_ CompletedResponse& response)
{
    InitializeLogging();

    LogTime();

	wstring expectedProcessPath;
    if (!GetExpectedProcessPath(SERVERNAME, expectedProcessPath))
    {
        FailWithGetLastError(IDS_GetExpectedProcessPathFailed);
    }

    // First attempt to grab the mutex
    wstring mutexName(expectedProcessPath);
    replace(mutexName.begin(), mutexName.end(), L'\\', L'/');

    Log(IDS_CreatingMutex);

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
        Log(IDS_TryingExistingProcesses);
        pipeHandle.reset(TryExistingProcesses(expectedProcessPath.c_str()));
        if (pipeHandle != nullptr)
        {
            Log(IDS_Connected);
            createProcessMutex.release();
            Log(IDS_Compiling);

            return TryCompile(pipeHandle.get(),
							  language,
							  currentDirectory,
							  commandLineArgs,
							  libEnvVar,
							  keepAlive,
							  response);
        }
        else
        {
            Log(IDS_CreatingNewProcess);
            processId = CreateNewServerProcess(expectedProcessPath.c_str());
            if (processId != 0)
            {
                LogFormatted(IDS_ConnectingToNewProcess, processId);
                pipeHandle.reset(ConnectToProcess(processId, TimeOutMsNewProcess));
                if (pipeHandle != nullptr)
                {
                    // Let everyone else access our process
                    Log(IDS_Connected);
                    createProcessMutex.release();
                    Log(IDS_Compiling);

                    return TryCompile(pipeHandle.get(),
									  language,
									  currentDirectory,
									  commandLineArgs,
									  libEnvVar,
									  keepAlive,
									  response);
                }
            }
        }

        createProcessMutex.release();
    }

	return false;
}

int RunCsc(
	_In_ const wstring& processPath,
	_In_ const list<wstring> args);

bool ProcessSlashes(_Inout_ WCHAR * & outBuffer, _Inout_ LPCWSTR * pszCur)
{
    // All this weird slash stuff follows the standard argument processing routines
    size_t iSlash = 0;
    LPCWSTR pCur = *pszCur;
    bool fIsQuoted = false;

    while (*pCur == L'\\')
        iSlash++, pCur++;

    if (*pCur == L'\"')
    {
        // Slashes followed by a quote character
        // put one slash in the output for every 2 slashes in the input
        for (; iSlash >= 2; iSlash -= 2)
        {
            *outBuffer = L'\\';
            outBuffer++;
        }

        // If there's 1 remaining slash, it's escaping the quote
        // so ignore the slash and keep the quote (as a normal character)
        if (iSlash & 1)
        { // Is it odd?
            *outBuffer = *pCur++;
            outBuffer++;
        }
        else
        {
            // A regular quote, so eat it and change the bQuoted
            pCur++;
            fIsQuoted = true;
        }
    }
    else
    {
        // Slashs not followed by a quote are just slashes
        for (; iSlash > 0; iSlash--)
        {
            *outBuffer = L'\\';
            outBuffer++;
        }
    }

    *pszCur = pCur;
    return fIsQuoted;
}

// Remove quote marks from a string
void RemoveQuotes(_Inout_z_ WCHAR * text)
{
    LPCWSTR pIn;
    WCHAR ch;

    pIn = text;
    for (;;)
    {
        switch (ch = *pIn)
        {
        case L'\0':
            // End of string. We're done.
            *text = L'\0';
            return;

        case L'\\':
            ProcessSlashes(text, &pIn);
            // Not break because ProcessSlashes has already advanced pIn
            continue;

        case L'\"':
            break;

        default:
            *text = ch;
            text++;
            break;
        }

        ++pIn;
    }
}


typedef  BOOL(__stdcall * SET_PREFERRED_UI_LANGUAGES_PROTOTYPE) (DWORD, PCWSTR, PULONG);

void SetPreferredUILangForMessages(LPCWSTR rawCommandLineArgs[], int argsCount, LPCWSTR uiDllname)
{
    list<wstring> commandLineArgs(rawCommandLineArgs, rawCommandLineArgs + argsCount);

    // Loop through the arguments to find the preferreduilang switch.
    for (auto iter = commandLineArgs.cbegin(); iter != commandLineArgs.cend(); iter++)
    {
        auto arg = *iter;

        if (!(arg[0] == '-' || arg[0] == '/'))
            continue;  // Not an option.

        if (_wcsnicmp(arg.c_str() + 1, L"preferreduilang:", 16) == 0)
        {
            size_t langidLength = arg.length() - (1 + 16);
            // The string will be terminated by two null chars - hence the +2.
            WCHAR *langid = new WCHAR[langidLength + 2];

            arg._Copy_s(langid, langidLength + 1, langidLength, 1 + 16);
            langid[langidLength] = L'\0';

            // remove quotes
            RemoveQuotes(langid);

            if (*langid != '\0')
            {
                HMODULE hKernel = GetModuleHandleA("kernel32.dll");
                if (hKernel)
                {
                    // SetProcessPreferredUILangs expects a string that is double null terminated and has a list of ui langs
                    // separated by a null character. So for en-us the string should be "en-us\0\0".
                    langidLength = wcslen(langid);
                    langid[langidLength + 1] = '\0';

                    SET_PREFERRED_UI_LANGUAGES_PROTOTYPE pfnSetProcessPreferredUILanguages =
                        (SET_PREFERRED_UI_LANGUAGES_PROTOTYPE)GetProcAddress(hKernel, "SetProcessPreferredUILanguages");
                    if (pfnSetProcessPreferredUILanguages != NULL)
                    {
                        BOOL success = pfnSetProcessPreferredUILanguages(MUI_LANGUAGE_NAME, langid, NULL);
                        if (success)
                        {
                            HINSTANCE hinstMessages = GetMessageDll(uiDllname);

                            if (hinstMessages)
                            {
                                g_hinstMessages = hinstMessages;
                            }
                        }
                    }
                }
            }

            delete[] langid;
        }
        else
            continue;       // Not a recognized argument.
    }
}

int Run(RequestLanguage language)
{
    try
    {
        LPCWSTR uiDllname = L"vbcsc2ui.dll";
		LPCWSTR clientExeName = language == RequestLanguage::CSHARPCOMPILE
			? L"csc.exe" : L"vbc.exe";
        g_hinstMessages = GetMessageDll(uiDllname);

        if (!g_hinstMessages)
        {
            // Fall back to this module if none was found.
            g_hinstMessages = GetModuleHandle(NULL);
        }

        auto currentDirectory = GetCurrentDirectory();
        int commandLineCount;
        auto commandLine = GetCommandLineArgs(commandLineCount);
		// Omit process name
		auto rawArgs = commandLine.get() + 1;
		auto rawArgsCount = commandLineCount - 1;
        wstring libEnvVariable;

        // Change stderr, stdout to binary, because the output we get from the server already 
        // has CR and LF in it. If we don't do this, we get CR CR LF at each newline.
        (void)_setmode(_fileno(stdout), _O_BINARY);
        (void)_setmode(_fileno(stderr), _O_BINARY);

        // Process the /preferreduilang switch and refetch the resource dll
        SetPreferredUILangForMessages(
            rawArgs,
            rawArgsCount,
            uiDllname);

		// Get the args without the native client-specific arguments
		list<wstring> argsList(rawArgs, rawArgs + rawArgsCount);
		wstring keepAlive;
		// Throws FatalError if parsing fails
		ParseAndValidateClientArguments(argsList, keepAlive);

		// Try to use the compiler server
		CompletedResponse response;
		if (TryRunServerCompilation(
				language,
				currentDirectory.c_str(),
				argsList,
				keepAlive,
				GetEnvVar(L"LIB", libEnvVariable) ? libEnvVariable.c_str() : nullptr,
				response))
		{
			OutputResponse(response);
			return response.ExitCode;
		}

		// Fallback to csc.exe
		wstring processPath;
		if (!GetExpectedProcessPath(clientExeName, processPath))
		{
			throw FatalError(GetResourceString(IDS_CreateClientProcessFailed));
		}
		return RunCsc(processPath, argsList);
    }
    catch (FatalError &e)
    {
        OutputWideString(stderr, e.message, true);
    }
    return 1;
}