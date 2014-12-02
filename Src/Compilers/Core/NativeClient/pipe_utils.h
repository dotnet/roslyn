#pragma once

#include <Windows.h>

// An abstract base class around a pipe that allows for a testing
// mock to be inserted
class IPipe
{
public:
    // Returns true if the write succeeds, false otherwise.
    virtual bool Write(_In_ LPCVOID data, unsigned size) = 0;
    // Returns true if the read succeeds, false otherwise.
    virtual bool Read(_Out_ LPVOID data, unsigned size) = 0;
};

// Delegates to a real pipe handle
class RealPipe : public IPipe
{
private:
    HANDLE pipeHandle;
public:
    RealPipe(HANDLE pipeHandle);
    virtual bool Write(_In_ LPCVOID, unsigned size);
    virtual bool Read(_Out_ LPVOID, unsigned size);
};

// Try opening the named pipe with the given name.
HANDLE OpenPipe(LPTSTR pipeName, DWORD retryOpenTimeoutMs);
