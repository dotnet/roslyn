#include "stdafx.h"
#include "pipe_utils.h"
#include "logging.h"
#include "UIStrings.h"

using namespace std;

RealPipe::RealPipe(HANDLE pipeHandle)
{
    this->pipeHandle = pipeHandle;
}

bool RealPipe::Write(_In_ LPCVOID data, unsigned toWrite)
{
    DWORD written;
    BOOL success = WriteFile(this->pipeHandle, data, toWrite, &written, nullptr);
    if (!success)
    {
        LogWin32Error(IDS_WriteFileOnPipeFailed);
        return false;
    }
    else if (written != toWrite)
    {
        LogFormatted(IDS_WriteFileOnPipeIncomplete, toWrite, written);
        return false;
    }
    return true;
}

#pragma warning(suppress: 6101)
bool RealPipe::Read(_Out_ LPVOID data, unsigned toRead)
{
    if (toRead == 0)
        return true;

    DWORD read;
    BOOL success = ReadFile(this->pipeHandle, data, toRead, &read, nullptr);
    if (!success)
    {
        LogWin32Error(IDS_ReadFileOnPipeFailed);
        return false;
    }
    else if (read != toRead)
    {
        LogFormatted(IDS_ReadFileOnPipeIncomplete, toRead, read);
        return false;
    }
    return true;
}

const DWORD MinConnectionAttempts = 3;        // Always make at least three attempts (matters when each attempt takes a long time (under load)).

// Try opening the named pipe with the given name.
// Retry up to "retryOpenTimeoutMs" milliseconds.
HANDLE OpenPipe(LPTSTR szPipeName, DWORD retryOpenTimeoutMs)
{
    // Try up to retryOpenTimeoutMs if:
    //   PIPE_BUSY occurs
    //   FILE_NOT_FOUND occurs .
    // Other errors causes us to fail immediately.

#pragma warning(suppress: 28159)
    DWORD startTicks = GetTickCount();
    DWORD currentTicks = startTicks;
    // Loop 3 times or until we hit the timeout, whichever is LONGER.
    for (int attempt = 0; attempt < MinConnectionAttempts || currentTicks - startTicks < retryOpenTimeoutMs; attempt++)
    {
        LogFormatted(IDS_AttemptToOpenNamedPipe, szPipeName);

        HANDLE pipeHandle = CreateFile(
            szPipeName,
            GENERIC_READ | GENERIC_WRITE,
            0, // share mode
            NULL, // security attributes
            OPEN_EXISTING,
            0, // default attributes
            NULL); // no template file

        if (pipeHandle != INVALID_HANDLE_VALUE)
        {
            LogFormatted(IDS_OpenedPipe, szPipeName, pipeHandle);

            return pipeHandle;
        }

        // Something went wrong. If we couldn't find the pipe, then possibly the process is still starting.
        DWORD errorCode = GetLastError();

        if (errorCode == ERROR_PIPE_BUSY)
        {
            Log(IDS_NamedPipeBusy);
            // All pipe instances are busy. Wait for one to become available. 
            if (!WaitNamedPipe(szPipeName, retryOpenTimeoutMs))
            {
                Log(IDS_NamedPipeWaitFailed);
                //EDMAURER the wait timed out. Give up.
                return INVALID_HANDLE_VALUE;
            }
        }
        else if (errorCode == ERROR_FILE_NOT_FOUND)
        {
            // Perhaps the server is still starting. Give it just a fraction of a second to start.
            Log(IDS_PipeNotFound);
            Sleep(100);
        }
        else
        {
            LogWin32Error(IDS_OpeningNamedPipe);
            return INVALID_HANDLE_VALUE;
        }

#pragma warning(suppress: 28159)
        currentTicks = GetTickCount();
        if (startTicks > currentTicks)
        {
            // Reset startTicks on overflow
            startTicks = 0;
        }
    }

#pragma warning(suppress: 28159)
    LogFormatted(IDS_PipeFindRetryFailed, GetTickCount() - startTicks);
    return INVALID_HANDLE_VALUE;
}
