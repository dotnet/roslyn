#include "logging.h"

#pragma once

class SmartHandle
{
private:
    HANDLE handle;

	static void close(HANDLE handle);

public:
	SmartHandle(HANDLE h);
	void reset(HANDLE h);
	bool operator==(const nullptr_t) const;
	bool operator!=(const nullptr_t) const;
	HANDLE get();
	~SmartHandle();
};

class SmartMutex
{
private:
    HANDLE handle;
	bool holdsMutex;

public:
	SmartMutex(LPCWSTR mutexName);
	bool HoldsMutex();
	bool Wait(const int waitTime);
	HANDLE get();
	void release();
	~SmartMutex();
};