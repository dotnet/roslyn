#include "stdafx.h"
#include "logging.h"
#include "protocol.h"
#include "UIStrings.h"
#include "smart_resources.h"
#include <sstream>

int RunCsc(
	_In_ const wstring& processPath,
	_In_ const list<wstring> args)
{
	SECURITY_ATTRIBUTES attr;
	attr.nLength = sizeof(SECURITY_ATTRIBUTES);
	attr.bInheritHandle = TRUE;
	attr.lpSecurityDescriptor = NULL;

	// Get stdout and stderr to pass to csc.exe
	auto stdOut = GetStdHandle(STD_OUTPUT_HANDLE);
	auto stdIn = GetStdHandle(STD_INPUT_HANDLE);
	auto stdErr = GetStdHandle(STD_ERROR_HANDLE);

	// Duplicate the handles for inheritance
	HANDLE outDup;
	HANDLE inDup;
	HANDLE errDup;

	auto thisHandle = GetCurrentProcess();

	DuplicateHandle(
		thisHandle,
		stdOut,
		thisHandle,
		&outDup,
		0,
		TRUE,
		DUPLICATE_SAME_ACCESS);

	DuplicateHandle(
		thisHandle,
		stdIn,
		thisHandle,
		&inDup,
		0,
		TRUE,
		DUPLICATE_SAME_ACCESS);

	DuplicateHandle(
		thisHandle,
		stdErr,
		thisHandle,
		&errDup,
		0,
		TRUE,
		DUPLICATE_SAME_ACCESS);

	PROCESS_INFORMATION procInfo = {};
	STARTUPINFO startInfo = {};
	BOOL success = FALSE;

	startInfo.cb = sizeof(STARTUPINFO);
	startInfo.hStdOutput = outDup;
	startInfo.hStdInput = inDup;
	startInfo.hStdError = errDup;
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

	if (success)
	{
		// Wait for the process to exit and return the exit code
		LogFormatted(IDS_CreatedProcess, procInfo.dwProcessId);
		WaitForSingleObject(procInfo.hProcess, INFINITE);

		DWORD exitCode = -1;
		GetExitCodeProcess(procInfo.hProcess, &exitCode);

		// Cleanup
		CloseHandle(procInfo.hProcess);
		CloseHandle(procInfo.hThread);

		return exitCode;
	}
	else
	{
		throw FatalError(GetResourceString(IDS_CreateClientProcessFailed));
	}
}