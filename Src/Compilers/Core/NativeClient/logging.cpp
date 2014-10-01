// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#include "stdafx.h"
#include "logging.h"
#include <memory>
#include <string>
#include "UIStrings.h"

using namespace std;

// This file implements some logging function that make debugging a lot easier
// The format and environment variable is shared by the client and server pieces
// so a single log file is written to by both processes.

static FILE * logFile = nullptr;

bool HaveLogFile()
{
	return logFile != nullptr;
}

wstring GetResourceString(UINT loadResource)
{
    extern HINSTANCE g_hinstMessages;
    LPWSTR tempStr;
    int result = LoadString(g_hinstMessages, loadResource, (LPWSTR)&tempStr, 0);

    if (result > 0)
    {
        return wstring(tempStr, result);
    }

    return wstring(L"");
}


bool GetEnvVar(_In_z_ LPCWSTR name, _Out_ wstring &value)
{
	value.clear();
	auto sizeNeeded = GetEnvironmentVariableW(name, nullptr, 0);
	if (sizeNeeded != 0)
	{
		value.resize(sizeNeeded);

		auto written = GetEnvironmentVariableW(name, &value[0], sizeNeeded);
		if (written != 0 && written <= sizeNeeded)
		{
			value.resize(written);
			return true;
		}
	}

	return false;
}

void InitializeLogging()
{
	wstring loggingFileName;
	if (GetEnvVar(LOGGING_ENV_VAR, loggingFileName))
	{
		// If the environment variable contains the path of a currently existing directory,
		// then use a process-specific name for the log file and put it in that directory.
		// Otherwise, assume that the environment variable specifies the name of the log file.
		DWORD attributes = GetFileAttributesW(loggingFileName.c_str());
		if (attributes != INVALID_FILE_ATTRIBUTES &&
			((attributes & FILE_ATTRIBUTE_DIRECTORY) == FILE_ATTRIBUTE_DIRECTORY))
		{
			loggingFileName += L"\\client.";
			loggingFileName += to_wstring((int)GetCurrentProcessId());
			loggingFileName += L".";
#pragma warning(suppress: 28159)
			loggingFileName += to_wstring(GetTickCount());
			loggingFileName += L".log";
		}
		logFile = _wfsopen(loggingFileName.c_str(), L"at", _SH_DENYNO);
	}
}

static void LogPrefix()
{
#pragma warning(suppress: 28159)
	fprintf(logFile, "CLI PID=%u TID=%u Ticks=%u: ", GetCurrentProcessId(), GetCurrentThreadId(), GetTickCount());
}

void Log(UINT loadResource)
{
    Log(GetResourceString(loadResource).c_str());
}

void Log(_In_z_ LPCWSTR message)
{
	if (logFile != nullptr) 
	{
		LogPrefix();
		fwprintf(logFile, message);
		fwprintf(logFile, L"\r\n");
		fflush(logFile);
	}
}
 
static void vLogFormatted(_In_z_ LPCWSTR message, va_list varargs)
{
	if (logFile != nullptr)
	{
		LogPrefix();
		vfwprintf(logFile, message, varargs);
		fprintf(logFile, "\r\n");
		fflush(logFile);
	}
}

void LogFormatted(UINT loadResource, ...)
{
    va_list varargs;
    va_start(varargs, loadResource);
    LogFormatted(GetResourceString(loadResource).c_str(), varargs);
    va_end(varargs);
}

void LogFormatted(_In_z_ LPCWSTR message, ...)
{
	if (logFile != nullptr) 
	{
		va_list varargs;
		va_start(varargs, message);

		vLogFormatted(message, varargs);
		va_end(varargs);
	}
}

void LogTime()
{
    if (logFile != nullptr)
    {
        SYSTEMTIME time;
        GetLocalTime(&time);
        LogFormatted(IDS_FormattedLocalTime, time.wHour, time.wMinute, time.wSecond, time.wMilliseconds);
    }
}

void LogWin32Error(UINT loadResource)
{
    LogFormatted(GetResourceString(loadResource).c_str());
}

void LogWin32Error(_In_z_ LPCWSTR message)
{
    LogFormatted(IDS_LogWin32Error, GetLastError(), message);
}

void Exit(int exitCode)
{
    LogTime();
    LogFormatted(IDS_ExitingWithCode, exitCode);
    exit(exitCode);
}

void FailWithGetLastError(UINT loadResource)
{
    FailWithGetLastError(GetResourceString(loadResource).c_str());
}

void FailWithGetLastError(_In_z_ LPCWSTR optionalPrefix)
{
    LPWSTR errorMsg;
    if (!FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_ALLOCATE_BUFFER,
        nullptr,
        GetLastError(),
        0,
        (LPWSTR)&errorMsg,
        0,
        nullptr))
    {
        errorMsg = L"";
    }

    // TODO: How should this error be localized? Is there more information we could output for debugging purposes?
    if (optionalPrefix == nullptr)
        optionalPrefix = L"";

    wstring buffer = GetResourceString(IDS_InternalCompilerClientErrorPrefix);
    buffer += optionalPrefix;
    buffer += L" ";
    buffer += errorMsg;

    Log(buffer.c_str());
    LocalFree(errorMsg);
    throw FatalError(move(buffer));
}

void FailFormatted(UINT loadResource, ...)
{
    va_list varargs;
    va_start(varargs, loadResource);
    FailFormatted(GetResourceString(loadResource).c_str(), varargs);
    va_end(varargs);
}

void FailFormatted(_In_z_ LPCWSTR message, ...)
{
    va_list varargs;
    va_start(varargs, message);

    wstring fullMessage = GetResourceString(IDS_InternalCompilerClientErrorPrefix);

    int needed = _vscwprintf(message, varargs);
    auto buffer = std::make_unique <WCHAR[]>(needed + 1);
    _vsnwprintf_s(buffer.get(), needed + 1, _TRUNCATE, message, varargs);
    va_end(varargs);

    fullMessage += buffer.release();
    fullMessage += L"\r\n";

    Log(fullMessage.c_str());
    throw FatalError(move(fullMessage));
}