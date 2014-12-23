#include "stdafx.h"
#include "logging.h"
#include "protocol.h"
#include "UIStrings.h"
#include "smart_resources.h"
#include <sstream>

void ReadOutput(_In_ HANDLE outHandle, _Out_ vector<BYTE>& output);

int RunInProcCompiler(
    _In_ const wstring& processPath,
    _In_ const list<wstring> args,
    _Out_ vector<BYTE>& stdOut,
    _Out_ vector<BYTE>& stdErr)
{
    SECURITY_ATTRIBUTES attr;
    attr.nLength = sizeof(SECURITY_ATTRIBUTES);
    attr.bInheritHandle = TRUE;
    attr.lpSecurityDescriptor = NULL;

    // Get stdin to pass to csc.exe
    auto stdIn = GetStdHandle(STD_INPUT_HANDLE);

    // Create handles for the child to write to and the parent to read from
    HANDLE stdOutRead;
    HANDLE stdOutWrite;
    HANDLE stdErrRead;
    HANDLE stdErrWrite;
    HANDLE inDup;

    auto thisHandle = GetCurrentProcess();
    DuplicateHandle(
        thisHandle,
        stdIn,
        thisHandle,
        &inDup,
        0,
        TRUE,
        DUPLICATE_SAME_ACCESS);

    if (!CreatePipe(&stdOutRead, &stdOutWrite, &attr, 0))
    {
        FailWithGetLastError(GetResourceString(IDS_ConnectToInProcCompilerFailed));
    }

    if (!CreatePipe(&stdErrRead, &stdErrWrite, &attr, 0))
    {
        FailWithGetLastError(GetResourceString(IDS_ConnectToInProcCompilerFailed));
    }

    // Mark the read end of the pipes non-inheritable
    if (!SetHandleInformation(stdOutRead, HANDLE_FLAG_INHERIT, 0))
    {
        FailWithGetLastError(GetResourceString(IDS_ConnectToInProcCompilerFailed));
    }

    if (!SetHandleInformation(stdErrRead, HANDLE_FLAG_INHERIT, 0))
    {
        FailWithGetLastError(GetResourceString(IDS_ConnectToInProcCompilerFailed));
    }

    PROCESS_INFORMATION procInfo = {};
    STARTUPINFO startInfo = {};
    BOOL success = FALSE;

    startInfo.cb = sizeof(STARTUPINFO);
    startInfo.hStdOutput = stdOutWrite;
    startInfo.hStdInput = inDup;
    startInfo.hStdError = stdErrWrite;
    startInfo.dwFlags |= STARTF_USESTDHANDLES;

    // Assemble the command line
    wstringstream argsBuf;

    // Quote the exe path in case there are spaces
    argsBuf << '"';
    argsBuf << processPath;
    argsBuf << '"';

    // Space seperate every argument
    for (auto arg : args)
    {
        argsBuf << ' ';
        argsBuf << arg;
    }

    auto argsString = argsBuf.str();
    auto argsCpy = make_unique<wchar_t[]>(argsString.size() + 1);
    wcscpy_s(argsCpy.get(), argsString.size() + 1, argsString.c_str());

    // Create the child process. 
    success = CreateProcess(NULL,
        argsCpy.get(),     // command line 
        NULL,          // process security attributes 
        NULL,          // primary thread security attributes 
        TRUE,          // handles are inherited 
        NORMAL_PRIORITY_CLASS | CREATE_UNICODE_ENVIRONMENT,
        NULL,          // use parent's environment 
        NULL,          // use parent's current directory 
        &startInfo,  // STARTUPINFO pointer 
        &procInfo);  // receives PROCESS_INFORMATION 

    CloseHandle(stdOutWrite);
    CloseHandle(inDup);
    CloseHandle(stdErrWrite);

    if (success)
    {
        // Read stdout and stderr from the process
        ReadOutput(stdOutRead, stdOut);
        ReadOutput(stdErrRead, stdErr);

        // Wait for the process to exit and return the exit code
        LogFormatted(IDS_CreatedProcess, procInfo.dwProcessId);
        WaitForSingleObject(procInfo.hProcess, INFINITE);

        DWORD exitCode = -1;
        GetExitCodeProcess(procInfo.hProcess, &exitCode);

        // Cleanup
        CloseHandle(procInfo.hProcess);
        CloseHandle(procInfo.hThread);

        CloseHandle(stdOutRead);
        CloseHandle(stdErrRead);

        return exitCode;
    }
    else
    {
        CloseHandle(stdOutRead);
        CloseHandle(stdErrRead);

        FailWithGetLastError(GetResourceString(IDS_CreatingProcess));
        // Unreachable
        return -1;
    }
}

void ReadOutput(
    _In_ HANDLE handle,
    _Out_ vector<BYTE>& output)
{
    DWORD read;
    const int bufSize = 4096;
    BYTE buf[bufSize];
    BOOL success = false;
    output.clear();

    for (;;)
    {
        success = ReadFile(handle, buf, bufSize, &read, nullptr);
        if (!success || !read)
            break;
        output.insert(output.end(), buf, buf + read);
    }
}
