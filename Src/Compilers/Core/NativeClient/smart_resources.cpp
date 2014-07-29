#include "stdafx.h"
#include "smart_resources.h"

void SmartHandle::close(HANDLE handle)
{
	if (handle != nullptr && handle != INVALID_HANDLE_VALUE
		&& !CloseHandle(handle))
	{
        FailWithGetLastError(L"CloseHandle");
	}
}

SmartHandle::SmartHandle(HANDLE handle)
{
    this->handle = handle;
}

void SmartHandle::reset(HANDLE newHandle)
{
    HANDLE old_handle = this->handle;
    this->handle = newHandle;
    close(old_handle);
}

bool SmartHandle::operator==(const nullptr_t) const
{
    return handle == nullptr;
}

bool SmartHandle::operator!=(const nullptr_t) const
{
    return !(this->operator==(nullptr));
}

HANDLE SmartHandle::get()
{
	return handle;
}

SmartHandle::~SmartHandle()
{
    close(this->handle);
}

SmartMutex::SmartMutex(LPCWSTR mutexName)
{
	this->handle = CreateMutexW(nullptr,
		                        TRUE, /* initially owned */
								mutexName);

    // If we fail to create the mutex this spells bad news for everything mutex
    // related. We can only log the error and continue without it.
    if (this->handle == nullptr)
    {
        LogWin32Error(L"Failure to create mutex");
    }

	this->holdsMutex = this->handle != nullptr && GetLastError() == ERROR_SUCCESS;
}

bool SmartMutex::HoldsMutex()
{
	return this->holdsMutex;
}

bool SmartMutex::Wait(const int waitTime)
{
    Log(L"Waiting for mutex.");
	auto mutexError = WaitForSingleObject(this->handle, waitTime);
	this->holdsMutex = false;
    switch (mutexError)
    {
    case WAIT_ABANDONED:
        Log(L"Acquired mutex, but mutex was previously abandoned");
        this->holdsMutex = true;
        break;
    case WAIT_OBJECT_0:
        Log(L"Acquired mutex.");
        this->holdsMutex = true;
        break;
    case WAIT_TIMEOUT:
        Log(L"Waiting for mutex timed out");
        break;
    case WAIT_FAILED:
        LogWin32Error(L"Waiting on the mutex failed");
        break;
    default:
        LogFormatted(L"Unknown WaitForSingleObject mutex failure %d, return code not documented\n", mutexError);
        break;
    }
	return this->holdsMutex;
}

HANDLE SmartMutex::get()
{
	return handle;
}

void SmartMutex::release()
{
    if (handle != nullptr && holdsMutex)
    {
		if (!ReleaseMutex(handle))
		{
			Log(L"Error releasing mutex");
		}
		else
		{
			holdsMutex = false;
		}
    }
}

SmartMutex::~SmartMutex()
{
	release();
	if (handle != nullptr)
	{
		CloseHandle(handle);
	}
}