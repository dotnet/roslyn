// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#include "stdafx.h"
#include "logging.h"

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
//
// The same code is used for both VB and C#; the tiny differences are controlled
// by a preprocessor constants COMPILE_VISUALBASIC or COMPILE_CSHARP

#if ! (defined(COMPILE_VISUALBASIC) || defined(COMPILE_CSHARP) || defined(COMPILE_ANALYZE))
#error "Must define a constant to chose build request type
#endif

const DWORD MinConnectionAttempts = 3;        // Always make at least three attempts (matters when each attempt takes a long time (under load)).
const DWORD TimeOutMsExistingProcess = 2000;  // Spend up to 2s connecting to existing process (existing processes should be always responsive).
const DWORD TimeOutMsNewProcess = 60000;      // Spend up to 60s connection to new process, to allow time for it to start.

// Set to true if /utf8output flag is set.
BOOL g_utf8Output = FALSE;

void Fail(char* optionalPrefix = NULL)
{
	LPWSTR errorMsg;	//Don't bother to free this. The process is terminating.
	FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM | FORMAT_MESSAGE_ALLOCATE_BUFFER,
		NULL,
		GetLastError(),
		0,
		(LPWSTR)&errorMsg,
		0,
		NULL);

	// TODO: How should this error be localized? Is there more information we could output for debugging purposes?
	if (!optionalPrefix)
		optionalPrefix = "";

	fprintf(stdout, "Internal Compiler Client Error: %ws %ws\r\n", optionalPrefix, errorMsg);
	LogFormatted("Internal Compiler Client Error: %ws %ws\r\n", optionalPrefix, errorMsg);
	Exit(1);
}

// Encapsulate the response we got from the server.
class Response
{
public:
	int exitCode;
	LPTSTR output;
	LPTSTR errorOutput;

	Response(int exitCode, LPTSTR output, LPTSTR errorOutput)
	{
		this->exitCode = exitCode;
		this->output = output;
		this->errorOutput = errorOutput;
	}
};


// Try opening the named pipe with the given name.
// Retry up to "retryOpenTimeoutMs" milliseconds.
HANDLE OpenPipe(LPTSTR szPipeName, DWORD retryOpenTimeoutMs)
{
	// Try up to retryOpenTimeoutMs if:
	//   PIPE_BUSY occurs
	//   FILE_NOT_FOUND occurs .
	// Other errors causes us to fail immediately.

	DWORD startTicks = GetTickCount();
	DWORD currentTicks = startTicks;
	// Loop 3 times or until we hit the timeout, whichever is LONGER.
	for (int attempt = 0; attempt < MinConnectionAttempts || currentTicks - startTicks < retryOpenTimeoutMs; attempt++) 
	{
		LogFormatted("Attempt to open named pipe '%ws'", szPipeName);

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
			LogFormatted("Successfully opened pipe '%ws' as handle %d", szPipeName, pipeHandle);

			return pipeHandle;
		}

		// Something went wrong. If we couldn't find the pipe, then possibly the process is still starting.
		DWORD errorCode = GetLastError();

		if (errorCode == ERROR_PIPE_BUSY) 
		{
			Log("Named pipe is busy.");
			// All pipe instances are busy. Wait for one to become available. 
			if (!WaitNamedPipe(szPipeName, retryOpenTimeoutMs))
			{
				Log("Named pipe wait failed.");
				//EDMAURER the wait timed out. Give up.
				return INVALID_HANDLE_VALUE;
			}
		}
		else if (errorCode == ERROR_FILE_NOT_FOUND) 
		{
			// Perhaps the server is still starting. Give it just a fraction of a second to start.
			Log("Pipe not found. Sleeping.");
			Sleep(100);
		}
		else 
		{
			LogWin32Error("Opening named pipe");
			return INVALID_HANDLE_VALUE;
		}

		currentTicks = GetTickCount();
		if (startTicks > currentTicks)
		{
			// Reset startTicks on overflow
			startTicks = 0;
		}
	}

	LogFormatted("Pipe not found after retrying for %d ms.", GetTickCount() - startTicks);
	return INVALID_HANDLE_VALUE;
}

// Get the expected process path of the server. We assume that the server EXE
// will be in the same directory as the client EXE. This allows us to support
// side-by-side install of different compilers. We only connect to servers that
// have the expected full process path.
bool GetExpectedProcessPath(LPTSTR szProcessName, int cch)
{
	if (GetModuleFileName(NULL, szProcessName, cch) != 0)
	{
		TCHAR * lastBackslash = wcsrchr(szProcessName, '\\');
		if (lastBackslash != NULL) 
		{
			*(lastBackslash + 1) = '\0';
			if (FAILED(StringCchCat(szProcessName, cch, SERVERNAME)))
				return false;

			return true;
		}
	}

	return false;
}

// Get the full name of a process.
bool GetProcessName(DWORD processID, LPTSTR szProcessName, int cch)
{
	// Get a handle to the process.
	bool success = false;
	HANDLE processHandle = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, FALSE, processID);

	// Get the process name.
	if (NULL != processHandle)
	{
		DWORD length = cch;
		if (QueryFullProcessImageName(processHandle, 0, szProcessName, &length))
		{
			success = true;
		}

		// Release the handle to the process.
		if (!CloseHandle(processHandle))
			Fail("CloseHandle");
	}

	return success;
}

bool WriteDataToPipe(HANDLE pipeHandle, LPVOID pData, DWORD cbData)
{
	DWORD cbWritten;
	BOOL success = WriteFile(pipeHandle, pData, cbData, &cbWritten, NULL);
	if (success && cbWritten == cbData)
	{
		return true;
	}
	else 
	{
		LogFormatted("WriteFile problem: success=%d, cbWritten=%d, cbData=%d", success, cbWritten, cbData);
		LogWin32Error("writing data to pipe");
		return false;
	}
}

bool ReadDataFromPipe(HANDLE pipeHandle, LPVOID pData, DWORD cbData)
{
	if (cbData == 0)
		return true;

	DWORD cbRead;
	BOOL success = ReadFile(pipeHandle, pData, cbData, &cbRead, NULL);
	if (success && cbRead == cbData)
	{
		return true;
	}
	else 
	{
		LogFormatted("ReadFile problem: success=%d, cbRead=%d, cbData=%d", success, cbRead, cbData);
		LogWin32Error("reading data from pipe");
		return false;
	}
}

LPWSTR ReadStringFromPipe(HANDLE pipeHandle)
{
	int cch;
	LPWSTR sz;
	if (!ReadDataFromPipe(pipeHandle, &cch, sizeof(cch)))
		return NULL;

	LogFormatted("String length = %d", cch);

	sz = new WCHAR[cch + 1];
	if (sz == NULL)
		return NULL;

	if (!ReadDataFromPipe(pipeHandle, sz, cch * sizeof(WCHAR)))
	{
		delete[] sz;
		return NULL;
	}

	sz[cch] = 0;  // null terminate.

	return sz;
}

// We use a request buffer to format an entire request. We do this because its more efficient,
// and also we need to know the full size of the request first.
class RequestBuffer
{
private:
	BYTE * requestBuffer;
	int currentSize;
	int currentCapacity;

public:
	RequestBuffer()
	{
		currentCapacity = 8;
		requestBuffer = (BYTE *)malloc(currentCapacity);
		currentSize = 0;
	}

	~RequestBuffer()
	{
		if (requestBuffer != NULL)
		{
			free(requestBuffer);
			requestBuffer = NULL;
			currentCapacity = currentSize = 0;
		}
	}

	void AddData(LPVOID pData, int cData)
	{
		while (cData + currentSize > currentCapacity) 
		{
			currentCapacity *= 2;
		}

        requestBuffer = (BYTE *)realloc(requestBuffer, currentCapacity);

        if (requestBuffer == NULL)
        {
			Fail("Out of memory");
            Exit(100);
        }

		memcpy(requestBuffer + currentSize, pData, cData);
		currentSize += cData;
	}

	void AddString(LPWSTR str)
	{
		int cch = wcslen(str);

		AddData(&cch, sizeof(cch));
		AddData(str, cch * sizeof(WCHAR));
	}

	void AddArgument(int argumentId, int argumentIndex, LPWSTR value)
	{
		AddData(&argumentId, sizeof(argumentId));
		AddData(&argumentIndex, sizeof(argumentId));
		AddString(value);
	}

	// Write the request buffer to the pipe, prefixed by its length.
	bool WriteToPipe(HANDLE pipeHandle)
	{
		LogFormatted("Writing request of size %d", currentSize);
		if (!WriteDataToPipe(pipeHandle, &currentSize, sizeof(currentSize)))
			return false;
		if (!WriteDataToPipe(pipeHandle, requestBuffer, currentSize))
			return false;
		return true;
	}
};


// Write the request to the server.
bool WriteRequest(HANDLE pipeHandle)
{
	TCHAR szCurrentDirectory[MAX_PATH];

	if (0 == GetCurrentDirectory(MAX_PATH, szCurrentDirectory)) 
	{
		LogWin32Error("GetCurrentDirectory");
		return false;
	}

	LPTSTR szCommandLine = GetCommandLine();
	int cCommandLineArgs;
	LPWSTR * aszCommandLineArgs = CommandLineToArgvW(szCommandLine, &cCommandLineArgs);
	if (aszCommandLineArgs == NULL) 
	{
		LogWin32Error("CommandLineToArgvW");
		return false;
	}

	// "LIB" environment variable.
	LPWSTR szLibEnvVariable = NULL;
	size_t cLibEnvVariable;
	_wdupenv_s(&szLibEnvVariable, &cLibEnvVariable, L"LIB");

	Log("Formatting request");

	RequestBuffer req;

#if defined(COMPILE_CSHARP)
	int requestType = REQUESTID_CSHARPCOMPILE;
#elif defined(COMPILE_VISUALBASIC)
	int requestType = REQUESTID_VBCOMPILE;
#elif defined(COMPILE_ANALYZE)
	int requestType = REQUESTID_ANALYZE;
#endif
	int cArguments = (cCommandLineArgs - 1) + 1;  // number of arguments we're sending.
	if (szLibEnvVariable != NULL)
		cArguments += 1;

	req.AddData(&requestType, sizeof(requestType));
	req.AddData(&cArguments, sizeof(cArguments));

	// Current directory.
	req.AddArgument(ARGUMENTID_CURRENTDIRECTORY, 0, szCurrentDirectory);

	// "LIB" environment variable.
	if (szLibEnvVariable != NULL)
	{
		req.AddArgument(ARGUMENTID_LIBENVVARIABLE, 0, szLibEnvVariable);
	}

	// Command line arguments (ignore argument 0.)
	for (int iArg = 1; iArg < cCommandLineArgs; ++iArg)
	{
		LPWSTR arg = aszCommandLineArgs[iArg];
		req.AddArgument(ARGUMENTID_COMMANDLINEARGUMENT, iArg - 1, arg);

		// The /utf8output option must be handled on the client side. 
		if (wcscmp(arg, L"/utf8output") == 0 || wcscmp(arg, L"-utf8output") == 0)
		{
			g_utf8Output = TRUE;
		}
	}

	Log("Writing request");
	if (!req.WriteToPipe(pipeHandle))
	{
		LogWin32Error("WritingToPipe failed");
		return false;
	}

	Log("Request written successfully");
	return true;
}

// Read response from the server. Returns NULL on error.
Response * ReadResponse(HANDLE pipeHandle)
{
	Log("Reading response");

	int returnCode = 0;
	int sizeInBytes;
	LPTSTR output = NULL;
	LPTSTR errorOutput = NULL;

	if (!ReadDataFromPipe(pipeHandle, &sizeInBytes, sizeof(sizeInBytes)))
		return NULL;
	LogFormatted("Response has %d bytes", sizeInBytes);

	if (!ReadDataFromPipe(pipeHandle, &returnCode, sizeof(returnCode)))
		return NULL;
	LogFormatted("Return code=%d", returnCode);

	output = ReadStringFromPipe(pipeHandle);
	if (output == NULL)
		return NULL;

	errorOutput = ReadStringFromPipe(pipeHandle);
	if (errorOutput == NULL)
	{
		delete[] output;
		return NULL;
	}

	Log("Response read successfully");

	return new Response(returnCode, output, errorOutput);
}

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

	if (!GetConsoleMode(hFile, &dwMode))
	{
		return false;
	}

	return true;
}

// Output a unicode string, taking into account console code pages 
// and possible /utf8output options.
void OutputWideString(FILE * outputFile, LPWSTR str)
{
	UINT cp;
	int cchStr = wcslen(str);

	if (!IsConsole(outputFile) && g_utf8Output)
	{
		cp = CP_UTF8;
	}
	else 
	{
		cp = GetConsoleOutputCP();
	}

	int bytesNeeded = WideCharToMultiByte(cp, 0, str, cchStr, NULL, 0, NULL, NULL);
	LPSTR outputBuffer = new CHAR[bytesNeeded];
	WideCharToMultiByte(cp, 0, str, cchStr, outputBuffer, bytesNeeded, NULL, NULL);
	fwrite(outputBuffer, 1, bytesNeeded, outputFile);
	delete[] outputBuffer;
}

// Output the response we got back from the server onto our stdout and stderr.
void OutputResponse(Response * response)
{
	OutputWideString(stdout, response->output);
	OutputWideString(stderr, response->errorOutput);
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
		Log("Sucessfully opened pipe");
		return pipeHandle;
	}

	Log("Failed to open pipe - can try another server process.");
	return NULL;
}

// Perform the compilation. If the compilation completes, this function
// never returns, exiting with the exit code of the compilation.
void DoCompilation(HANDLE pipeHandle)
{
	if (!WriteRequest(pipeHandle)) 
	{
		Log("Failed to write request - can try another server process.");
		if (!CloseHandle(pipeHandle))
			Fail("CloseHandle");

		return;
	}

	Log("Successfully wrote request.");

	Response * response = ReadResponse(pipeHandle);
	if (!CloseHandle(pipeHandle))
		Fail("CloseHandle");

	if (response != NULL) 
	{
		Log("Successfully read response.");

		// We got a response.
		OutputResponse(response);

		Exit(response->exitCode);
	}

	Log("Response was null - can try another server process.");
}

// Get the process ids of all processes on the system.
DWORD * GetAllProcessIds(int * pcProcesses)
{
	Log("Enumerating all process IDs");

	DWORD * pProcesses, cbNeeded, cProcesses;
	int nProcesses = 1;
	for (;;)
	{
		pProcesses = new DWORD[nProcesses];
		if (pProcesses == NULL)
			return NULL;

		if (!EnumProcesses(pProcesses, nProcesses * sizeof(DWORD), &cbNeeded))
		{
			delete[] pProcesses;
			LogWin32Error("EnumProcesses");
			return NULL;
		}

		// Calculate how many process identifiers were returned.
		cProcesses = cbNeeded / sizeof(DWORD);
		if (cProcesses != nProcesses)
			break;

		delete[] pProcesses;
		nProcesses *= 2;
	} 

	*pcProcesses = cProcesses;
	return pProcesses;
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
	LPCTSTR suffix = _T("\\amd64\\managed");
	LPCTSTR installRoot = _T("COMPLUS_InstallRoot");
	LPCTSTR toolPath = _T("RazzleToolPath");

	int bufLength = MAX_PATH;
	LPTSTR buffer = (LPTSTR)malloc(bufLength * sizeof(TCHAR));

	if (buffer == NULL) goto err_oom;

	int written = GetEnvironmentVariable(
		toolPath,
		buffer,
		bufLength);

	if (written == 0)
	{
		free(buffer);
		return;
	}

	if (written >= bufLength)
	{
		bufLength = written;
		buffer = (LPTSTR)realloc(buffer, bufLength * sizeof(TCHAR));

		if (buffer == NULL) goto err_oom;

		written = GetEnvironmentVariable(
			toolPath,
			buffer,
			bufLength);
	}

	int suffixLen = _tcslen(suffix);

	if (suffixLen + written > bufLength)
	{
		bufLength = suffixLen;
		buffer = (LPTSTR)realloc(buffer, bufLength * sizeof(TCHAR));

		if (buffer == NULL) goto err_oom;
	}

	int hr = StringCchCat(buffer, bufLength, suffix);
	if (FAILED(hr))
	{
		LogFormatted("StringCChCat failed with error %d", hr);
		Exit(1);
	}

	if (!SetEnvironmentVariable(installRoot, buffer))
		Fail("SetEnvironmentVariable install root");
	if (!SetEnvironmentVariable(_T("COMPLUS_Version"), _T("v4.5"))) 
		Fail("SetEnvironmentVariable version");

	free(buffer);
	return;

err_oom:
	Fail("SetupDevDivEnvironment -- out of memory");
	Exit(100);
}

// Start a new server process with the given executable name,
// and return the process id of the process. On error, return
// zero.
DWORD CreateNewServerProcess(LPTSTR executableName)
{
	STARTUPINFO startupInfo;
	PROCESS_INFORMATION processInfo;
	BOOL success;

	LogFormatted("Attempting to create process '%ws'", executableName);

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

	success = CreateProcess(executableName, 
		NULL, // command line,
		NULL, // process attributes
		NULL, // thread attributes
		FALSE, // don't inherit handles
		NORMAL_PRIORITY_CLASS | CREATE_NO_WINDOW | CREATE_UNICODE_ENVIRONMENT,
		NULL, // Inherit environment
		NULL, // inherit current directory
		&startupInfo,
		&processInfo);

	if (success) 
	{
		// We don't need the process and thread handles.
		LogFormatted("Successfully created process with process id %d", processInfo.dwProcessId);
		CloseHandle(processInfo.hProcess);
		CloseHandle(processInfo.hThread);
		return processInfo.dwProcessId;
	}
	else 
	{
		LogWin32Error("Creating process");
		return 0;
	}
}

HANDLE TryExistingProcesses(LPCTSTR expectedProcessName)
{
	DWORD processID;
	DWORD * pProcesses;
	int cProcesses;

	pProcesses = GetAllProcessIds(&cProcesses);
	if (pProcesses != NULL)
	{
		LogFormatted("Found %d processes", cProcesses);

		// Check each process to find one with the right name.
		for (int i = 0; i < cProcesses; i++)
		{
			if (pProcesses[i] != 0)
			{
				processID = pProcesses[i];
				TCHAR szProcessName[MAX_PATH];
				bool success = GetProcessName(processID, szProcessName, MAX_PATH);
				if (success) 
				{
					if (lstrcmpi(szProcessName, expectedProcessName) == 0) 
					{
						LogFormatted("Found process '%ws' with process id %d", szProcessName, processID); 
						HANDLE pipeHandle = ConnectToProcess(processID, TimeOutMsExistingProcess);
						if (pipeHandle != NULL)
						{
							delete[] pProcesses;
							return pipeHandle;
						}
					}
				}
			}
		}
		delete[] pProcesses;
	}
	return NULL;
}

int _tmain(int argc, _TCHAR* argv[])
{
	TCHAR expectedProcessPath[MAX_PATH];

	InitializeLogging();

	LogTime();

	// Change stderr, stdout to binary, because the output we get from the server already 
	// has CR and LF in it. If we don't do this, we get CR CR LF at each newline.
	_setmode(_fileno(stdout), _O_BINARY);
	_setmode(_fileno(stderr), _O_BINARY);

	if (!GetExpectedProcessPath(expectedProcessPath, MAX_PATH))
	{
		Fail("GetExpectedProcessPath");
	}

	// Didn't connect to a process successfully. Try creating a process instead.

    // First attempt to grab the mutex
	TCHAR mutexName[MAX_PATH];
	if (0 == _tcsncpy_s(mutexName, expectedProcessPath, MAX_PATH))
	{
		for (TCHAR * c = mutexName; *c != _T('\0'); ++c)
		{
			if (*c == _T('\\')) *c = _T('/');
		}
	}
	else
	{
		Log("Failed to copy process name.");
	}

	Log("Creating mutex.");

	HANDLE createProcessMutex = CreateMutex(NULL, TRUE, mutexName);
	DWORD mutexError = GetLastError();
	bool holdsMutex = createProcessMutex != NULL && mutexError == ERROR_SUCCESS;

    // If we fail to create the mutex this spells bad news for everything mutex
    // related. We can only log the error and continue without it.
	if (createProcessMutex == NULL)
	{
		LogWin32Error("Failure to create mutex");
	}

    // If the mutex already exists and someone else has it, we should wait
	if (!holdsMutex)
	{
		Log("Waiting for mutex.");
		mutexError = WaitForSingleObject(createProcessMutex, TimeOutMsNewProcess);
		switch (mutexError)
		{
		case WAIT_ABANDONED:
			Log("Acquired mutex, but mutex was previously abandoned");
			holdsMutex = true;
			break;
		case WAIT_OBJECT_0:
			Log("Acquired mutex.");
			holdsMutex = true;
			break;
		case WAIT_TIMEOUT:
			Log("Waiting for mutex timed out");
			break;
		case WAIT_FAILED:
			LogWin32Error("Waiting on the mutex failed");
			break;
		default:
			LogFormatted("Unknown WaitForSingleObject mutex failure %d, return code not documented\n", mutexError);
			break;
		}
	}

    // Proceed with the mutex
	if (holdsMutex)
	{
		// Check for already running processes in case someone came in before us
		Log("Trying existing processes.");
		HANDLE pipeHandle = TryExistingProcesses(expectedProcessPath);
		if (pipeHandle != NULL)
		{
			Log("Connected, releasing mutex.");
			if (!ReleaseMutex(createProcessMutex))
			{
				Log("Error releasing mutex.");
			}
			Log("Compiling.");
			// Never returns if we succeed.
			DoCompilation(pipeHandle);

			Log("Compilation failed with existing process, retrying once.");
		}
		else
		{
			Log("No success with existing processes - try creating a new one.");
			DWORD processID = CreateNewServerProcess(expectedProcessPath);
			if (processID != 0)
			{
				LogFormatted("Connecting to newly created process id %d", processID);
				HANDLE pipeHandle = ConnectToProcess(processID, TimeOutMsNewProcess);
				if (pipeHandle != NULL)
				{
					// Let everyone else access our process
					Log("Connected, releasing mutex.");
					if (!ReleaseMutex(createProcessMutex))
					{
						Log("Error releasing mutex");
					}
					holdsMutex = false;
					Log("Compiling.");
					// This should never come back
					DoCompilation(pipeHandle);
				}
			}

			Log("No success with created process, retrying once.");
		}

		if (holdsMutex && !ReleaseMutex(createProcessMutex))
		{
			Log("Error releasing mutex");
		}

        // Sleep shortly before retying in case the failure was due to
        // resource contention
		Sleep(500);
	}

	// Try one time without a mutex
	Log("Trying without mutex");
	DWORD processID = CreateNewServerProcess(expectedProcessPath);
	if (processID != 0)
	{
		LogFormatted("Connecting to newly created process id %d", processID);
		HANDLE pipeHandle = ConnectToProcess(processID, TimeOutMsNewProcess);
		if (pipeHandle != NULL)
		{
			// Let everyone else access our process
			Log("Connected to new process.");
			DoCompilation(pipeHandle);
		}
	}

	Fail();
	return 1;
}
