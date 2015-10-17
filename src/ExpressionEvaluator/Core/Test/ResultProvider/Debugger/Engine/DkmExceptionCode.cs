// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\Concord\Microsoft.VisualStudio.Debugger.Engine.dll

#endregion
namespace Microsoft.VisualStudio.Debugger
{
    //
    // Summary:
    //     Defines the HRESULT codes used by this API.
    public enum DkmExceptionCode
    {
        //
        // Summary:
        //     Unspecified error.
        E_FAIL = -2147467259,
        //
        // Summary:
        //     A debugger is already attached.
        E_ATTACH_DEBUGGER_ALREADY_ATTACHED = -2147221503,
        //
        // Summary:
        //     The process does not have sufficient privileges to be debugged.
        E_ATTACH_DEBUGGEE_PROCESS_SECURITY_VIOLATION = -2147221502,
        //
        // Summary:
        //     The desktop cannot be debugged.
        E_ATTACH_CANNOT_ATTACH_TO_DESKTOP = -2147221501,
        //
        // Summary:
        //     Unmanaged debugging is not available.
        E_LAUNCH_NO_INTEROP = -2147221499,
        //
        // Summary:
        //     Debugging isn't possible due to an incompatibility within the CLR implementation.
        E_LAUNCH_DEBUGGING_NOT_POSSIBLE = -2147221498,
        //
        // Summary:
        //     Visual Studio cannot debug managed applications because a kernel debugger is
        //     enabled on the system. Please see Help for further information.
        E_LAUNCH_KERNEL_DEBUGGER_ENABLED = -2147221497,
        //
        // Summary:
        //     Visual Studio cannot debug managed applications because a kernel debugger is
        //     present on the system. Please see Help for further information.
        E_LAUNCH_KERNEL_DEBUGGER_PRESENT = -2147221496,
        //
        // Summary:
        //     The debugger does not support debugging managed and native code at the same time
        //     on the platform of the target computer/device. Configure the debugger to debug
        //     only native code or only managed code.
        E_INTEROP_NOT_SUPPORTED = -2147221495,
        //
        // Summary:
        //     The maximum number of processes is already being debugged.
        E_TOO_MANY_PROCESSES = -2147221494,
        //
        // Summary:
        //     Script debugging of your application is disabled in Internet Explorer. To enable
        //     script debugging in Internet Explorer, choose Internet Options from the Tools
        //     menu and navigate to the Advanced tab. Under the Browsing category, clear the
        //     'Disable Script Debugging (Internet Explorer)' checkbox, then restart Internet
        //     Explorer.
        E_MSHTML_SCRIPT_DEBUGGING_DISABLED = -2147221493,
        //
        // Summary:
        //     The correct version of pdm.dll is not registered. Repair your Visual Studio installation,
        //     or run 'regsvr32.exe "%CommonProgramFiles%\Microsoft Shared\VS7Debug\pdm.dll"'.
        E_SCRIPT_PDM_NOT_REGISTERED = -2147221492,
        //
        // Summary:
        //     The .NET debugger has not been installed properly. The most probable cause is
        //     that mscordbi.dll is not properly registered. Click Help for more information
        //     on how to repair the .NET debugger.
        E_DE_CLR_DBG_SERVICES_NOT_INSTALLED = -2147221491,
        //
        // Summary:
        //     There is no managed code running in the process. In order to attach to a process
        //     with the .NET debugger, managed code must be running in the process before attaching.
        E_ATTACH_NO_CLR_PROGRAMS = -2147221490,
        //
        // Summary:
        //     The Microsoft Visual Studio Remote Debugging Monitor has been closed on the remote
        //     machine.
        E_REMOTE_SERVER_CLOSED = -2147221488,
        //
        // Summary:
        //     The Microsoft Visual Studio Remote Debugging Monitor on the remote computer does
        //     not support debugging code running in the Common Language Runtime.
        E_CLR_NOT_SUPPORTED = -2147221482,
        //
        // Summary:
        //     The Microsoft Visual Studio Remote Debugging Monitor on the remote computer does
        //     not support debugging code running in the Common Language Runtime on a 64-bit
        //     computer.
        E_64BIT_CLR_NOT_SUPPORTED = -2147221481,
        //
        // Summary:
        //     Cannot debug minidumps and processes at the same time.
        E_CANNOT_MIX_MINDUMP_DEBUGGING = -2147221480,
        E_DEBUG_ENGINE_NOT_REGISTERED = -2147221479,
        //
        // Summary:
        //     This application has failed to start because the application configuration is
        //     incorrect. Review the manifest file for possible errors. Reinstalling the application
        //     may fix this problem. For more details, please see the application event log.
        E_LAUNCH_SXS_ERROR = -2147221478,
        //
        // Summary:
        //     Failed to initialize msdbg2.dll for script debugging. If this problem persists,
        //     use 'Add or Remove Programs' in Control Panel to repair your Visual Studio installation.
        E_FAILED_TO_INITIALIZE_SCRIPT_PROXY = -2147221477,
        //
        // Summary:
        //     The Microsoft Visual Studio Remote Debugging Monitor (MSVSMON.EXE) does not appear
        //     to be running on the remote computer. This may be because a firewall is preventing
        //     communication to the remote computer. Please see Help for assistance on configuring
        //     remote debugging.
        E_REMOTE_SERVER_DOES_NOT_EXIST = -2147221472,
        //
        // Summary:
        //     Access is denied. Can not connect to Microsoft Visual Studio Remote Debugging
        //     Monitor on the remote computer.
        E_REMOTE_SERVER_ACCESS_DENIED = -2147221471,
        //
        // Summary:
        //     The debugger cannot connect to the remote computer. The debugger was unable to
        //     resolve the specified computer name.
        E_REMOTE_SERVER_MACHINE_DOES_NOT_EXIST = -2147221470,
        //
        // Summary:
        //     The debugger is not properly installed. Run setup to install or repair the debugger.
        E_DEBUGGER_NOT_REGISTERED_PROPERLY = -2147221469,
        //
        // Summary:
        //     Access is denied. This seems to be because the 'Network access: Sharing and security
        //     model for local accounts' security policy does not allow users to authenticate
        //     as themselves. Please use the 'Local Security Settings' administration tool on
        //     the local computer to configure this option.
        E_FORCE_GUEST_MODE_ENABLED = -2147221468,
        E_GET_IWAM_USER_FAILURE = -2147221467,
        //
        // Summary:
        //     The specified remote server name is not valid.
        E_REMOTE_SERVER_INVALID_NAME = -2147221466,
        //
        // Summary:
        //     Microsoft Visual Studio Debugging Monitor (MSVSMON.EXE) failed to start. If this
        //     problem persists, please repair your Visual Studio installation via 'Add or Remove
        //     Programs' in Control Panel.
        E_AUTO_LAUNCH_EXEC_FAILURE = -2147221464,
        //
        // Summary:
        //     Microsoft Visual Studio Remote Debugging Monitor (MSVSMON.EXE) is not running
        //     under your user account and MSVSMON could not be automatically started. MSVSMON
        //     must be manually started, or the Visual Studio remote debugging components must
        //     be installed on the remote computer. Please see Help for assistance.
        E_REMOTE_COMPONENTS_NOT_REGISTERED = -2147221461,
        //
        // Summary:
        //     A DCOM error occurred trying to contact the remote computer. Access is denied.
        //     It may be possible to avoid this error by changing your settings to debug only
        //     native code or only managed code.
        E_DCOM_ACCESS_DENIED = -2147221460,
        //
        // Summary:
        //     Debugging using the Default transport is not possible because the remote machine
        //     has 'Share-level access control' enabled. To enable debugging on the remote machine,
        //     go to Control Panel -> Network -> Access control, and set Access control to be
        //     'User-level access control'.
        E_SHARE_LEVEL_ACCESS_CONTROL_ENABLED = -2147221459,
        //
        // Summary:
        //     Logon failure: unknown user name or bad password. See help for more information.
        E_WORKGROUP_REMOTE_LOGON_FAILURE = -2147221458,
        //
        // Summary:
        //     Windows authentication is disabled in the Microsoft Visual Studio Remote Debugging
        //     Monitor (MSVSMON). To connect, choose one of the following options. 1. Enable
        //     Windows authentication in MSVSMON 2. Reconfigure your project to disable Windows
        //     authentication 3. Use the 'Remote (native with no authentication)' transport
        //     in the 'Attach to Process' dialog
        E_WINAUTH_CONNECT_NOT_SUPPORTED = -2147221457,
        //
        // Summary:
        //     A previous expression evaluation is still in progress.
        E_EVALUATE_BUSY_WITH_EVALUATION = -2147221456,
        //
        // Summary:
        //     The expression evaluation took too long.
        E_EVALUATE_TIMEOUT = -2147221455,
        //
        // Summary:
        //     Mixed-mode debugging does not support Microsoft.NET Framework versions earlier
        //     than 2.0.
        E_INTEROP_CLR_TOO_OLD = -2147221454,
        //
        // Summary:
        //     Check for one of the following. 1. The application you are trying to debug uses
        //     a version of the Microsoft .NET Framework that is not supported by the debugger.
        //     2. The debugger has made an incorrect assumption about the Microsoft .NET Framework
        //     version your application is going to use. 3. The Microsoft .NET Framework version
        //     specified by you for debugging is incorrect. Please see the Visual Studio .NET
        //     debugger documentation for correctly specifying the Microsoft .NET Framework
        //     version your application is going to use for debugging.
        E_CLR_INCOMPATIBLE_PROTOCOL = -2147221453,
        //
        // Summary:
        //     Unable to attach because process is running in fiber mode.
        E_CLR_CANNOT_DEBUG_FIBER_PROCESS = -2147221452,
        //
        // Summary:
        //     Visual Studio has insufficient privileges to debug this process. To debug this
        //     process, Visual Studio must be run as an administrator.
        E_PROCESS_OBJECT_ACCESS_DENIED = -2147221451,
        //
        // Summary:
        //     Visual Studio has insufficient privileges to inspect the process's identity.
        E_PROCESS_TOKEN_ACCESS_DENIED = -2147221450,
        //
        // Summary:
        //     Visual Studio was unable to inspect the process's identity. This is most likely
        //     due to service configuration on the computer running the process.
        E_PROCESS_TOKEN_ACCESS_DENIED_NO_TS = -2147221449,
        E_OPERATION_REQUIRES_ELEVATION = -2147221448,
        //
        // Summary:
        //     Visual Studio has insufficient privileges to debug this process. To debug this
        //     process, Visual Studio must be run as an administrator.
        E_ATTACH_REQUIRES_ELEVATION = -2147221447,
        E_MEMORY_NOTSUPPORTED = -2147221440,
        //
        // Summary:
        //     The type of code you are currently debugging does not support disassembly.
        E_DISASM_NOTSUPPORTED = -2147221439,
        //
        // Summary:
        //     The specified address does not exist in disassembly.
        E_DISASM_BADADDRESS = -2147221438,
        E_DISASM_NOTAVAILABLE = -2147221437,
        //
        // Summary:
        //     The breakpoint has been deleted.
        E_BP_DELETED = -2147221408,
        //
        // Summary:
        //     The process has been terminated.
        E_PROCESS_DESTROYED = -2147221392,
        E_PROCESS_DEBUGGER_IS_DEBUGGEE = -2147221391,
        //
        // Summary:
        //     Terminating this process is not allowed.
        E_TERMINATE_FORBIDDEN = -2147221390,
        //
        // Summary:
        //     The thread has terminated.
        E_THREAD_DESTROYED = -2147221387,
        //
        // Summary:
        //     Cannot find port. Check the remote machine name.
        E_PORTSUPPLIER_NO_PORT = -2147221376,
        E_PORT_NO_REQUEST = -2147221360,
        E_COMPARE_CANNOT_COMPARE = -2147221344,
        E_JIT_INVALID_PID = -2147221327,
        E_JIT_VSJITDEBUGGER_NOT_REGISTERED = -2147221325,
        E_JIT_APPID_NOT_REGISTERED = -2147221324,
        E_JIT_RUNTIME_VERSION_UNSUPPORTED = -2147221322,
        E_SESSION_TERMINATE_DETACH_FAILED = -2147221310,
        E_SESSION_TERMINATE_FAILED = -2147221309,
        //
        // Summary:
        //     Detach is not supported on Microsoft Windows 2000 for native code.
        E_DETACH_NO_PROXY = -2147221296,
        E_DETACH_TS_UNSUPPORTED = -2147221280,
        E_DETACH_IMPERSONATE_FAILURE = -2147221264,
        //
        // Summary:
        //     This thread has called into a function that cannot be displayed.
        E_CANNOT_SET_NEXT_STATEMENT_ON_NONLEAF_FRAME = -2147221248,
        E_TARGET_FILE_MISMATCH = -2147221247,
        E_IMAGE_NOT_LOADED = -2147221246,
        E_FIBER_NOT_SUPPORTED = -2147221245,
        //
        // Summary:
        //     The next statement cannot be set to another function.
        E_CANNOT_SETIP_TO_DIFFERENT_FUNCTION = -2147221244,
        //
        // Summary:
        //     In order to Set Next Statement, right-click on the active frame in the Call Stack
        //     window and select "Unwind To This Frame".
        E_CANNOT_SET_NEXT_STATEMENT_ON_EXCEPTION = -2147221243,
        //
        // Summary:
        //     The next statement cannot be changed until the current statement has completed.
        E_ENC_SETIP_REQUIRES_CONTINUE = -2147221241,
        //
        // Summary:
        //     The next statement cannot be set from outside a finally block to within it.
        E_CANNOT_SET_NEXT_STATEMENT_INTO_FINALLY = -2147221240,
        //
        // Summary:
        //     The next statement cannot be set from within a finally block to a statement outside
        //     of it.
        E_CANNOT_SET_NEXT_STATEMENT_OUT_OF_FINALLY = -2147221239,
        //
        // Summary:
        //     The next statement cannot be set from outside a catch block to within it.
        E_CANNOT_SET_NEXT_STATEMENT_INTO_CATCH = -2147221238,
        //
        // Summary:
        //     The next statement cannot be changed at this time.
        E_CANNOT_SET_NEXT_STATEMENT_GENERAL = -2147221237,
        //
        // Summary:
        //     The next statement cannot be set into or out of a catch filter.
        E_CANNOT_SET_NEXT_STATEMENT_INTO_OR_OUT_OF_FILTER = -2147221236,
        //
        // Summary:
        //     This process is not currently executing the type of code that you selected to
        //     debug.
        E_ASYNCBREAK_NO_PROGRAMS = -2147221232,
        //
        // Summary:
        //     The debugger is still attaching to the process or the process is not currently
        //     executing the type of code selected for debugging.
        E_ASYNCBREAK_DEBUGGEE_NOT_INITIALIZED = -2147221231,
        //
        // Summary:
        //     The debugger is handling debug events or performing evaluations that do not allow
        //     nested break state. Try again.
        E_ASYNCBREAK_UNABLE_TO_PROCESS = -2147221230,
        //
        // Summary:
        //     The web server has been locked down and is blocking the DEBUG verb, which is
        //     required to enable debugging. Please see Help for assistance.
        E_WEBDBG_DEBUG_VERB_BLOCKED = -2147221215,
        //
        // Summary:
        //     ASP debugging is disabled because the ASP process is running as a user that does
        //     not have debug permissions. Please see Help for assistance.
        E_ASP_USER_ACCESS_DENIED = -2147221211,
        //
        // Summary:
        //     The remote debugging components are not registered or running on the web server.
        //     Ensure the proper version of msvsmon is running on the remote computer.
        E_AUTO_ATTACH_NOT_REGISTERED = -2147221210,
        //
        // Summary:
        //     An unexpected DCOM error occurred while trying to automatically attach to the
        //     remote web server. Try manually attaching to the remote web server using the
        //     'Attach To Process' dialog.
        E_AUTO_ATTACH_DCOM_ERROR = -2147221209,
        //
        // Summary:
        //     Expected failure from web server CoCreating debug verb CLSID
        E_AUTO_ATTACH_COCREATE_FAILURE = -2147221208,
        E_AUTO_ATTACH_CLASSNOTREG = -2147221207,
        //
        // Summary:
        //     The current thread cannot continue while an expression is being evaluated on
        //     another thread.
        E_CANNOT_CONTINUE_DURING_PENDING_EXPR_EVAL = -2147221200,
        E_REMOTE_REDIRECTION_UNSUPPORTED = -2147221195,
        //
        // Summary:
        //     The specified working directory does not exist or is not a full path.
        E_INVALID_WORKING_DIRECTORY = -2147221194,
        //
        // Summary:
        //     The application manifest has the uiAccess attribute set to 'true'. Running an
        //     Accessibility application requires following the steps described in Help.
        E_LAUNCH_FAILED_WITH_ELEVATION = -2147221193,
        //
        // Summary:
        //     This program requires additional permissions to start. To debug this program,
        //     restart Visual Studio as an administrator.
        E_LAUNCH_ELEVATION_REQUIRED = -2147221192,
        //
        // Summary:
        //     Cannot locate Microsoft Internet Explorer.
        E_CANNOT_FIND_INTERNET_EXPLORER = -2147221191,
        //
        // Summary:
        //     The Visual Studio Remote Debugger (MSVSMON.EXE) has insufficient privileges to
        //     debug this process. To debug this process, the remote debugger must be run as
        //     an administrator.
        E_REMOTE_PROCESS_OBJECT_ACCESS_DENIED = -2147221190,
        //
        // Summary:
        //     The Visual Studio Remote Debugger (MSVSMON.EXE) has insufficient privileges to
        //     debug this process. To debug this process, launch the remote debugger using 'Run
        //     as administrator'. If the remote debugger has been configured to run as a service,
        //     ensure that it is running under an account that is a member of the Administrators
        //     group.
        E_REMOTE_ATTACH_REQUIRES_ELEVATION = -2147221189,
        //
        // Summary:
        //     This program requires additional permissions to start. To debug this program,
        //     launch the Visual Studio Remote Debugger (MSVSMON.EXE) using 'Run as administrator'.
        E_REMOTE_LAUNCH_ELEVATION_REQUIRED = -2147221188,
        //
        // Summary:
        //     The attempt to unwind the callstack failed. Unwinding is not possible in the
        //     following scenarios: 1. Debugging was started via Just-In-Time debugging. 2.
        //     An unwind is in progress. 3. A System.StackOverflowException or System.Threading.ThreadAbortException
        //     exception has been thrown.
        E_EXCEPTION_CANNOT_BE_INTERCEPTED = -2147221184,
        //
        // Summary:
        //     You can only unwind to the function that caused the exception.
        E_EXCEPTION_CANNOT_UNWIND_ABOVE_CALLBACK = -2147221183,
        //
        // Summary:
        //     Unwinding from the current exception is not supported.
        E_INTERCEPT_CURRENT_EXCEPTION_NOT_SUPPORTED = -2147221182,
        //
        // Summary:
        //     You cannot unwind from an unhandled exception while doing managed and native
        //     code debugging at the same time.
        E_INTERCEPT_CANNOT_UNWIND_LASTCHANCE_INTEROP = -2147221181,
        E_JMC_CANNOT_SET_STATUS = -2147221179,
        //
        // Summary:
        //     The process has been terminated.
        E_DESTROYED = -2147220991,
        //
        // Summary:
        //     The Microsoft Visual Studio Remote Debugging Monitor is either not running on
        //     the remote machine or is running in Windows authentication mode.
        E_REMOTE_NOMSVCMON = -2147220990,
        //
        // Summary:
        //     The IP address for the remote machine is not valid.
        E_REMOTE_BADIPADDRESS = -2147220989,
        //
        // Summary:
        //     The remote machine is not responding.
        E_REMOTE_MACHINEDOWN = -2147220988,
        //
        // Summary:
        //     The remote machine name is not specified.
        E_REMOTE_MACHINEUNSPECIFIED = -2147220987,
        //
        // Summary:
        //     Other programs cannot be debugged during the current mixed dump debugging session.
        E_CRASHDUMP_ACTIVE = -2147220986,
        //
        // Summary:
        //     All of the threads are frozen. Use the Threads window to unfreeze at least one
        //     thread before attempting to step or continue the process.
        E_ALL_THREADS_SUSPENDED = -2147220985,
        //
        // Summary:
        //     The debugger transport DLL cannot be loaded.
        E_LOAD_DLL_TL = -2147220984,
        //
        // Summary:
        //     mspdb110.dll cannot be loaded.
        E_LOAD_DLL_SH = -2147220983,
        //
        // Summary:
        //     MSDIS170.dll cannot be loaded.
        E_LOAD_DLL_EM = -2147220982,
        //
        // Summary:
        //     NatDbgEE.dll cannot be loaded.
        E_LOAD_DLL_EE = -2147220981,
        //
        // Summary:
        //     NatDbgDM.dll cannot be loaded.
        E_LOAD_DLL_DM = -2147220980,
        //
        // Summary:
        //     Old version of DBGHELP.DLL found, does not support minidumps.
        E_LOAD_DLL_MD = -2147220979,
        //
        // Summary:
        //     Input or output cannot be redirected because the specified file is invalid.
        E_IOREDIR_BADFILE = -2147220978,
        //
        // Summary:
        //     Input or output cannot be redirected because the syntax is incorrect.
        E_IOREDIR_BADSYNTAX = -2147220977,
        //
        // Summary:
        //     The remote debugger is not an acceptable version.
        E_REMOTE_BADVERSION = -2147220976,
        //
        // Summary:
        //     This operation is not supported when debugging dump files.
        E_CRASHDUMP_UNSUPPORTED = -2147220975,
        //
        // Summary:
        //     The remote computer does not have a CLR version which is compatible with the
        //     remote debugging components. To install a compatible CLR version, see the instructions
        //     in the 'Remote Components Setup' page on the Visual Studio CD.
        E_REMOTE_BAD_CLR_VERSION = -2147220974,
        //
        // Summary:
        //     The specified file is an unrecognized or unsupported binary format.
        E_UNSUPPORTED_BINARY = -2147220971,
        //
        // Summary:
        //     The process has been soft broken.
        E_DEBUGGEE_BLOCKED = -2147220970,
        //
        // Summary:
        //     The Microsoft Visual Studio Remote Debugging Monitor on the remote computer is
        //     running as a different user.
        E_REMOTE_NOUSERMSVCMON = -2147220969,
        //
        // Summary:
        //     Stepping to or from system code on a machine running Windows 95/Windows 98/Windows
        //     ME is not allowed.
        E_STEP_WIN9xSYSCODE = -2147220968,
        E_INTEROP_ORPC_INIT = -2147220967,
        //
        // Summary:
        //     The 64-bit version of the Visual Studio Remote Debugging Monitor (MSVSMON.EXE)
        //     cannot be used to debug 32-bit processes or 32-bit dumps. Please use the 32-bit
        //     version instead.
        E_CANNOT_DEBUG_WIN32 = -2147220965,
        //
        // Summary:
        //     The 32-bit version of the Visual Studio Remote Debugging Monitor (MSVSMON.EXE)
        //     cannot be used to debug 64-bit processes or 64-bit dumps. Please use the 64-bit
        //     version instead.
        E_CANNOT_DEBUG_WIN64 = -2147220964,
        //
        // Summary:
        //     Mini-Dumps cannot be read on this system. Please use a Windows NT based system
        E_MINIDUMP_READ_WIN9X = -2147220963,
        //
        // Summary:
        //     Attaching to a process in a different terminal server session is not supported
        //     on this computer. Try remote debugging to the machine and running the Microsoft
        //     Visual Studio Remote Debugging Monitor in the process's session.
        E_CROSS_TSSESSION_ATTACH = -2147220962,
        //
        // Summary:
        //     A stepping breakpoint could not be set
        E_STEP_BP_SET_FAILED = -2147220961,
        //
        // Summary:
        //     The debugger transport DLL being loaded has an incorrect version.
        E_LOAD_DLL_TL_INCORRECT_VERSION = -2147220960,
        //
        // Summary:
        //     NatDbgDM.dll being loaded has an incorrect version.
        E_LOAD_DLL_DM_INCORRECT_VERSION = -2147220959,
        E_REMOTE_NOMSVCMON_PIPE = -2147220958,
        //
        // Summary:
        //     msdia120.dll cannot be loaded.
        E_LOAD_DLL_DIA = -2147220957,
        //
        // Summary:
        //     The dump file you opened is corrupted.
        E_DUMP_CORRUPTED = -2147220956,
        //
        // Summary:
        //     Mixed-mode debugging of x64 processes is not supported when using Microsoft.NET
        //     Framework versions earlier than 4.0.
        E_INTEROP_X64 = -2147220955,
        //
        // Summary:
        //     Debugging older format crashdumps is not supported.
        E_CRASHDUMP_DEPRECATED = -2147220953,
        //
        // Summary:
        //     Debugging managed-only minidumps is not supported. Specify 'Mixed' for the 'Debugger
        //     Type' in project properties.
        E_LAUNCH_MANAGEDONLYMINIDUMP_UNSUPPORTED = -2147220952,
        //
        // Summary:
        //     Debugging managed or mixed-mode minidumps is not supported on IA64 platforms.
        //     Specify 'Native' for the 'Debugger Type' in project properties.
        E_LAUNCH_64BIT_MANAGEDMINIDUMP_UNSUPPORTED = -2147220951,
        //
        // Summary:
        //     The remote tools are not signed correctly.
        E_DEVICEBITS_NOT_SIGNED = -2147220479,
        //
        // Summary:
        //     Attach is not enabled for this process with this debug type.
        E_ATTACH_NOT_ENABLED = -2147220478,
        //
        // Summary:
        //     The connection has been broken.
        E_REMOTE_DISCONNECT = -2147220477,
        //
        // Summary:
        //     The threads in the process cannot be suspended at this time. This may be a temporary
        //     condition.
        E_BREAK_ALL_FAILED = -2147220476,
        //
        // Summary:
        //     Access denied. Try again, then check your device for a prompt.
        E_DEVICE_ACCESS_DENIED_SELECT_YES = -2147220475,
        //
        // Summary:
        //     Unable to complete the operation. This could be because the device's security
        //     settings are too restrictive. Please use the Device Security Manager to change
        //     the settings and try again.
        E_DEVICE_ACCESS_DENIED = -2147220474,
        //
        // Summary:
        //     The remote connection to the device has been lost. Verify the device connection
        //     and restart debugging.
        E_DEVICE_CONNRESET = -2147220473,
        //
        // Summary:
        //     Unable to load the CLR. The target device does not have a compatible version
        //     of the CLR installed for the application you are attempting to debug. Verify
        //     that your device supports the appropriate CLR version and has that CLR installed.
        //     Some devices do not support automatic CLR upgrade.
        E_BAD_NETCF_VERSION = -2147220472,
        E_REFERENCE_NOT_VALID = -2147220223,
        E_PROPERTY_NOT_VALID = -2147220207,
        E_SETVALUE_VALUE_CANNOT_BE_SET = -2147220191,
        E_SETVALUE_VALUE_IS_READONLY = -2147220190,
        E_SETVALUEASREFERENCE_NOTSUPPORTED = -2147220189,
        E_CANNOT_GET_UNMANAGED_MEMORY_CONTEXT = -2147220127,
        E_GETREFERENCE_NO_REFERENCE = -2147220095,
        E_CODE_CONTEXT_OUT_OF_SCOPE = -2147220063,
        E_INVALID_SESSIONID = -2147220062,
        //
        // Summary:
        //     The Visual Studio Remote Debugger on the target computer cannot connect back
        //     to this computer. A firewall may be preventing communication via DCOM to the
        //     local computer. It may be possible to avoid this error by changing your settings
        //     to debug only native code or only managed code.
        E_SERVER_UNAVAILABLE_ON_CALLBACK = -2147220061,
        //
        // Summary:
        //     The Visual Studio Remote Debugger on the target computer cannot connect back
        //     to this computer. Authentication failed. It may be possible to avoid this error
        //     by changing your settings to debug only native code or only managed code.
        E_ACCESS_DENIED_ON_CALLBACK = -2147220060,
        //
        // Summary:
        //     The Microsoft Visual Studio Remote Debugging Monitor on the remote computer could
        //     not connect to this computer because there was no available authentication service.
        //     It may be possible to avoid this error by changing your settings to debug only
        //     native code or only managed code.
        E_UNKNOWN_AUTHN_SERVICE_ON_CALLBACK = -2147220059,
        E_NO_SESSION_AVAILABLE = -2147220058,
        E_CLIENT_NOT_LOGGED_ON = -2147220057,
        E_OTHER_USERS_SESSION = -2147220056,
        E_USER_LEVEL_ACCESS_CONTROL_REQUIRED = -2147220055,
        //
        // Summary:
        //     Can not evaluate script expressions while thread is stopped in the CLR.
        E_SCRIPT_CLR_EE_DISABLED = -2147220048,
        //
        // Summary:
        //     Server side-error occurred on sending debug HTTP request.
        E_HTTP_SERVERERROR = -2147219712,
        //
        // Summary:
        //     An authentication error occurred while communicating with the web server. Please
        //     see Help for assistance.
        E_HTTP_UNAUTHORIZED = -2147219711,
        //
        // Summary:
        //     Could not start ASP.NET debugging. More information may be available by starting
        //     the project without debugging.
        E_HTTP_SENDREQUEST_FAILED = -2147219710,
        //
        // Summary:
        //     The web server is not configured correctly. See help for common configuration
        //     errors. Running the web page outside of the debugger may provide further information.
        E_HTTP_FORBIDDEN = -2147219709,
        //
        // Summary:
        //     The server does not support debugging of ASP.NET or ATL Server applications.
        //     Click Help for more information on how to enable debugging.
        E_HTTP_NOT_SUPPORTED = -2147219708,
        //
        // Summary:
        //     Could not start ASP.NET or ATL Server debugging.
        E_HTTP_NO_CONTENT = -2147219707,
        //
        // Summary:
        //     The web server could not find the requested resource.
        E_HTTP_NOT_FOUND = -2147219706,
        //
        // Summary:
        //     The debug request could not be processed by the server due to invalid syntax.
        E_HTTP_BAD_REQUEST = -2147219705,
        //
        // Summary:
        //     You do not have permissions to debug the web server process. You need to either
        //     be running as the same user account as the web server, or have administrator
        //     privilege.
        E_HTTP_ACCESS_DENIED = -2147219704,
        //
        // Summary:
        //     Unable to connect to the web server. Verify that the web server is running and
        //     that incoming HTTP requests are not blocked by a firewall.
        E_HTTP_CONNECT_FAILED = -2147219703,
        E_HTTP_EXCEPTION = -2147219702,
        //
        // Summary:
        //     The web server did not respond in a timely manner. This may be because another
        //     debugger is already attached to the web server.
        E_HTTP_TIMEOUT = -2147219701,
        //
        // Summary:
        //     IIS does not list a web site that matches the launched URL.
        E_HTTP_SITE_NOT_FOUND = -2147219700,
        //
        // Summary:
        //     IIS does not list an application that matches the launched URL.
        E_HTTP_APP_NOT_FOUND = -2147219699,
        //
        // Summary:
        //     Debugging requires the IIS Management Console. To install, go to Control Panel->Programs->Turn
        //     Windows features on or off. Check Internet Information Services->Web Management
        //     Tools->IIS Management Console.
        E_HTTP_MANAGEMENT_API_MISSING = -2147219698,
        //
        // Summary:
        //     The IIS worker process for the launched URL is not currently running.
        E_HTTP_NO_PROCESS = -2147219697,
        E_64BIT_COMPONENTS_NOT_INSTALLED = -2147219632,
        //
        // Summary:
        //     The Visual Studio debugger cannot connect to the remote computer. Unable to initiate
        //     DCOM communication. Please see Help for assistance.
        E_UNMARSHAL_SERVER_FAILED = -2147219631,
        //
        // Summary:
        //     The Microsoft Visual Studio Remote Debugging Monitor on the remote computer cannot
        //     connect to the local computer. Unable to initiate DCOM communication. Please
        //     see Help for assistance.
        E_UNMARSHAL_CALLBACK_FAILED = -2147219630,
        //
        // Summary:
        //     The Visual Studio debugger cannot connect to the remote computer. An RPC policy
        //     is enabled on the local computer which prevents remote debugging. Please see
        //     Help for assistance.
        E_RPC_REQUIRES_AUTHENTICATION = -2147219627,
        //
        // Summary:
        //     The Microsoft Visual Studio Remote Debugging Monitor cannot logon to the local
        //     computer: unknown user name or bad password. It may be possible to avoid this
        //     error by changing your settings to debug only native code or only managed code.
        E_LOGON_FAILURE_ON_CALLBACK = -2147219626,
        //
        // Summary:
        //     The Visual Studio debugger cannot establish a DCOM connection to the remote computer.
        //     A firewall may be preventing communication via DCOM to the remote computer. It
        //     may be possible to avoid this error by changing your settings to debug only native
        //     code or only managed code.
        E_REMOTE_SERVER_UNAVAILABLE = -2147219625,
        E_REMOTE_CONNECT_USER_CANCELED = -2147219624,
        //
        // Summary:
        //     Windows file sharing has been configured so that you will connect to the remote
        //     computer using a different user name. This is incompatible with remote debugging.
        //     Please see Help for assistance.
        E_REMOTE_CREDENTIALS_PROHIBITED = -2147219623,
        //
        // Summary:
        //     Windows Firewall does not currently allow exceptions. Use Control Panel to change
        //     the Windows Firewall settings so that exceptions are allowed.
        E_FIREWALL_NO_EXCEPTIONS = -2147219622,
        //
        // Summary:
        //     Cannot add an application to the Windows Firewall exception list. Use the Control
        //     Panel to manually configure the Windows Firewall.
        E_FIREWALL_CANNOT_OPEN_APPLICATION = -2147219621,
        //
        // Summary:
        //     Cannot add a port to the Windows Firewall exception list. Use the Control Panel
        //     to manually configure the Windows Firewall.
        E_FIREWALL_CANNOT_OPEN_PORT = -2147219620,
        //
        // Summary:
        //     Cannot add 'File and Printer Sharing' to the Windows Firewall exception list.
        //     Use the Control Panel to manually configure the Windows Firewall.
        E_FIREWALL_CANNOT_OPEN_FILE_SHARING = -2147219619,
        //
        // Summary:
        //     Remote debugging is not supported.
        E_REMOTE_DEBUGGING_UNSUPPORTED = -2147219618,
        E_REMOTE_BAD_MSDBG2 = -2147219617,
        E_ATTACH_USER_CANCELED = -2147219616,
        //
        // Summary:
        //     Maximum packet length exceeded. If the problem continues, reduce the number of
        //     network host names or network addresses that are assigned to the computer running
        //     Visual Studio computer or to the target computer.
        E_REMOTE_PACKET_TOO_BIG = -2147219615,
        //
        // Summary:
        //     The target process is running a version of the Microsoft .NET Framework newer
        //     than this version of Visual Studio. Visual Studio cannot debug this process.
        E_UNSUPPORTED_FUTURE_CLR_VERSION = -2147219614,
        //
        // Summary:
        //     This version of Visual Studio does not support debugging code that uses Microsoft
        //     .NET Framework v1.0. Use Visual Studio 2008 or earlier to debug this process.
        E_UNSUPPORTED_CLR_V1 = -2147219613,
        //
        // Summary:
        //     Mixed-mode debugging of IA64 processes is not supported.
        E_INTEROP_IA64 = -2147219612,
        //
        // Summary:
        //     See help for common configuration errors. Running the web page outside of the
        //     debugger may provide further information.
        E_HTTP_GENERAL = -2147219611,
        //
        // Summary:
        //     IDebugCoreServer* implementation does not have a connection to the remote computer.
        //     This can occur in T-SQL debugging when there is no remote debugging monitor.
        E_REMOTE_NO_CONNECTION = -2147219610,
        //
        // Summary:
        //     The specified remote debugging proxy server name is invalid.
        E_REMOTE_INVALID_PROXY_SERVER_NAME = -2147219609,
        //
        // Summary:
        //     Operation is not permitted on IDebugCoreServer* implementation which has a weak
        //     connection to the remote msvsmon instance. Weak connections are used when no
        //     process is being debugged.
        E_REMOTE_WEAK_CONNECTION = -2147219608,
        //
        // Summary:
        //     Remote program providers are no longer supported before debugging begins (ex:
        //     process enumeration).
        E_REMOTE_PROGRAM_PROVIDERS_UNSUPPORTED = -2147219607,
        //
        // Summary:
        //     Connection request was rejected by the remote debugger. Ensure that the remote
        //     debugger is running in 'No Authentication' mode.
        E_REMOTE_REJECTED_NO_AUTH_REQUEST = -2147219606,
        //
        // Summary:
        //     Connection request was rejected by the remote debugger. Ensure that the remote
        //     debugger is running in 'Windows Authentication' mode.
        E_REMOTE_REJECTED_WIN_AUTH_REQUEST = -2147219605,
        //
        // Summary:
        //     The debugger was unable to create a localhost TCP/IP connection, which is required
        //     for 64-bit debugging.
        E_PSEUDOREMOTE_NO_LOCALHOST_TCPIP_CONNECTION = -2147219604,
        //
        // Summary:
        //     This operation requires the Windows Web Services API to be installed, and it
        //     is not currently installed on this computer.
        E_REMOTE_WWS_NOT_INSTALLED = -2147219603,
        //
        // Summary:
        //     This operation requires the Windows Web Services API to be installed, and it
        //     is not currently installed on this computer. To install Windows Web Services,
        //     please restart Visual Studio as an administrator on this computer.
        E_REMOTE_WWS_INSTALL_REQUIRES_ADMIN = -2147219602,
        //
        // Summary:
        //     The expression has not yet been translated to native machine code.
        E_FUNCTION_NOT_JITTED = -2147219456,
        E_NO_CODE_CONTEXT = -2147219455,
        //
        // Summary:
        //     A Microsoft .NET Framework component, diasymreader.dll, is not correctly installed.
        //     Please repair your Microsoft .NET Framework installation via 'Add or Remove Programs'
        //     in Control Panel.
        E_BAD_CLR_DIASYMREADER = -2147219454,
        //
        // Summary:
        //     Unable to load the CLR. If a CLR version was specified for debugging, check that
        //     it was valid and installed on the machine. If the problem persists, please repair
        //     your Microsoft .NET Framework installation via 'Programs and Features' in Control
        //     Panel.
        E_CLR_SHIM_ERROR = -2147219453,
        //
        // Summary:
        //     Unable to map the debug start page URL to a machine name.
        E_AUTOATTACH_WEBSERVER_NOT_FOUND = -2147219199,
        E_DBGEXTENSION_NOT_FOUND = -2147219184,
        E_DBGEXTENSION_FUNCTION_NOT_FOUND = -2147219183,
        E_DBGEXTENSION_FAULTED = -2147219182,
        E_DBGEXTENSION_RESULT_INVALID = -2147219181,
        E_PROGRAM_IN_RUNMODE = -2147219180,
        //
        // Summary:
        //     The remote procedure could not be debugged. This usually indicates that debugging
        //     has not been enabled on the server. See help for more information.
        E_CAUSALITY_NO_SERVER_RESPONSE = -2147219168,
        //
        // Summary:
        //     Please install the Visual Studio Remote Debugger on the server to enable this
        //     functionality.
        E_CAUSALITY_REMOTE_NOT_REGISTERED = -2147219167,
        //
        // Summary:
        //     The debugger failed to stop in the server process.
        E_CAUSALITY_BREAKPOINT_NOT_HIT = -2147219166,
        //
        // Summary:
        //     Unable to determine a stopping location. Verify symbols are loaded.
        E_CAUSALITY_BREAKPOINT_BIND_ERROR = -2147219165,
        //
        // Summary:
        //     Debugging this project is disabled. Debugging can be re-enabled from 'Start Options'
        //     under project properties.
        E_CAUSALITY_PROJECT_DISABLED = -2147219164,
        //
        // Summary:
        //     Unable to attach the debugger to TSQL code.
        E_NO_ATTACH_WHILE_DDD = -2147218944,
        //
        // Summary:
        //     Click Help for more information.
        E_SQLLE_ACCESSDENIED = -2147218943,
        //
        // Summary:
        //     Click Help for more information.
        E_SQL_SP_ENABLE_PERMISSION_DENIED = -2147218942,
        //
        // Summary:
        //     Click Help for more information.
        E_SQL_DEBUGGING_NOT_ENABLED_ON_SERVER = -2147218941,
        //
        // Summary:
        //     Click Help for more information.
        E_SQL_CANT_FIND_SSDEBUGPS_ON_CLIENT = -2147218940,
        //
        // Summary:
        //     Click Help for more information.
        E_SQL_EXECUTED_BUT_NOT_DEBUGGED = -2147218939,
        //
        // Summary:
        //     Click Help for more information.
        E_SQL_VDT_INIT_RETURNED_SQL_ERROR = -2147218938,
        E_ATTACH_FAILED_ABORT_SILENTLY = -2147218937,
        //
        // Summary:
        //     Click Help for more information.
        E_SQL_REGISTER_FAILED = -2147218936,
        E_DE_NOT_SUPPORTED_PRE_8_0 = -2147218688,
        E_PROGRAM_DESTROY_PENDING = -2147218687,
        //
        // Summary:
        //     The operation isn't supported for the Common Language Runtime version used by
        //     the process being debugged.
        E_MANAGED_FEATURE_NOTSUPPORTED = -2147218515,
        //
        // Summary:
        //     The Visual Studio Remote Debugger does not support this edition of Windows.
        E_OS_PERSONAL = -2147218432,
        //
        // Summary:
        //     Source server support is disabled because the assembly is partially trusted.
        E_SOURCE_SERVER_DISABLE_PARTIAL_TRUST = -2147218431,
        //
        // Summary:
        //     Operation is not supported on the platform of the target computer/device.
        E_REMOTE_UNSUPPORTED_OPERATION_ON_PLATFORM = -2147218430,
        //
        // Summary:
        //     Unable to load Visual Studio debugger component (vsdebugeng.dll). If this problem
        //     persists, repair your installation via 'Add or Remove Programs' in Control Panel.
        E_LOAD_VSDEBUGENG_FAILED = -2147218416,
        //
        // Summary:
        //     Unable to initialize Visual Studio debugger component (vsdebugeng.dll). If this
        //     problem persists, repair your installation via 'Add or Remove Programs' in Control
        //     Panel.
        E_LOAD_VSDEBUGENG_IMPORTS_FAILED = -2147218415,
        //
        // Summary:
        //     Unable to initialize Visual Studio debugger due to a configuration error. If
        //     this problem persists, repair your installation via 'Add or Remove Programs'
        //     in Control Panel.
        E_LOAD_VSDEBUGENG_CONFIG_ERROR = -2147218414,
        //
        // Summary:
        //     Failed to launch minidump. The minidump file is corrupt.
        E_CORRUPT_MINIDUMP = -2147218413,
        //
        // Summary:
        //     Unable to load a Visual Studio component (VSDebugScriptAgent110.dll). If the
        //     problem persists, repair your installation via 'Add or Remove Programs' in Control
        //     Panel.
        E_LOAD_SCRIPT_AGENT_LOCAL_FAILURE = -2147218412,
        //
        // Summary:
        //     Remote script debugging requires that the remote debugger is registered on the
        //     target computer. Run the Visual Studio Remote Debugger setup (rdbgsetup_<processor>.exe)
        //     on the target computer.
        E_LOAD_SCRIPT_AGENT_REMOTE_FAILURE = -2147218410,
        //
        // Summary:
        //     The debugger was unable to find the registration for the target application.
        //     If the problem persists, try uninstalling and then reinstalling this application.
        E_APPX_REGISTRATION_NOT_FOUND = -2147218409,
        //
        // Summary:
        //     Unable to find a Visual Studio component (VsDebugLaunchNotify.exe). For remote
        //     debugging, this file must be present on the target computer. If the problem persists,
        //     repair your installation via 'Add or Remove Programs' in Control Panel.
        E_VSDEBUGLAUNCHNOTIFY_NOT_INSTALLED = -2147218408,
        //
        // Summary:
        //     Windows 8 build# 8017 or higher is required to debug Windows Store apps.
        E_WIN8_TOO_OLD = -2147218404,
        E_THREAD_NOT_FOUND = -2147218175,
        //
        // Summary:
        //     Cannot auto-attach to the SQL Server, possibly because the firewall is configured
        //     incorrectly or auto-attach is forbidden by the operating system.
        E_CANNOT_AUTOATTACH_TO_SQLSERVER = -2147218174,
        E_OBJECT_OUT_OF_SYNC = -2147218173,
        E_PROCESS_ALREADY_CONTINUED = -2147218172,
        //
        // Summary:
        //     Debugging multiple GPU processes is not supported.
        E_CANNOT_DEBUG_MULTI_GPU_PROCS = -2147218171,
        //
        // Summary:
        //     No available devices supported by the selected debug engine. Please select a
        //     different engine.
        E_GPU_ADAPTOR_NOT_FOUND = -2147218170,
        //
        // Summary:
        //     A Microsoft Windows component is not correctly registered. Please ensure that
        //     the Desktop Experience is enabled in Server Manager -> Manage -> Add Server Roles
        //     and Features.
        E_WINDOWS_GRAPHICAL_SHELL_UNINSTALLED_ERROR = -2147218169,
        //
        // Summary:
        //     Windows 8 or higher was required for GPU debugging on the software emulator.
        //     For the most up-to-date information, please visit the link below. http://go.microsoft.com/fwlink/p/?LinkId=330081
        E_GPU_DEBUG_NOT_SUPPORTED_PRE_DX_11_1 = -2147218168,
        //
        // Summary:
        //     There is a configuration issue with the selected Debugging Accelerator Type.
        //     For information on specific Accelerator providers, visit http://go.microsoft.com/fwlink/p/?LinkId=323500
        E_GPU_DEBUG_CONFIG_ISSUE = -2147218167,
        //
        // Summary:
        //     Local debugging is not supported for the selected Debugging Accelerator Type.
        //     Use Remote Windows Debugger instead or change the Debugging Accelerator Type
        E_GPU_LOCAL_DEBUGGING_ERROR = -2147218166,
        //
        // Summary:
        //     The debug driver for the selected Debugging Accelerator Type is not installed
        //     on the target machine. For more information, visit http://go.microsoft.com/fwlink/p/?LinkId=323500
        E_GPU_LOAD_VSD3D_FAILURE = -2147218165,
        //
        // Summary:
        //     Timeout Detection and Recovery (TDR) must be disabled at the remote site. For
        //     more information search for 'TdrLevel' in MSDN or visit the link below. http://go.microsoft.com/fwlink/p/?LinkId=323500
        E_GPU_TDR_ENABLED_FAILURE = -2147218164,
        //
        // Summary:
        //     Remote debugger does not support mixed (managed and native) debugger type.
        E_CANNOT_REMOTE_DEBUG_MIXED = -2147218163,
        //
        // Summary:
        //     Background Task activation failed Please see Help for further information.
        E_BG_TASK_ACTIVATION_FAILED = -2147218162,
        //
        // Summary:
        //     This version of the Visual Studio Remote Debugger does not support this operation.
        //     Please upgrade to the latest version. http://go.microsoft.com/fwlink/p/?LinkId=219549
        E_REMOTE_VERSION = -2147218161,
        //
        // Summary:
        //     Unable to load a Visual Studio component (symbollocator.resources.dll). If the
        //     problem persists, repair your installation via 'Add or Remove Programs' in Control
        //     Panel.
        E_SYMBOL_LOCATOR_INSTALL_ERROR = -2147218160,
        //
        // Summary:
        //     The format of the PE module is invalid.
        E_INVALID_PE_FORMAT = -2147218159,
        //
        // Summary:
        //     This dump is already being debugged.
        E_DUMP_ALREADY_LAUNCHED = -2147218158,
        //
        // Summary:
        //     The next statement cannot be set because the current assembly is optimized.
        E_CANNOT_SET_NEXT_STATEMENT_IN_OPTIMIZED_CODE = -2147218157,
        //
        // Summary:
        //     Debugging of ARM minidumps requires Windows 8 or above.
        E_ARMDUMP_NOT_SUPPORTED_PRE_WIN8 = -2147218156,
        //
        // Summary:
        //     Cannot detach while process termination is in progress.
        E_CANNOT_DETACH_WHILE_TERMINATE_IN_PROGRESS = -2147218155,
        //
        // Summary:
        //     A required Microsoft Windows component, wldp.dll could not be found on the target
        //     device.
        E_WLDP_NOT_FOUND = -2147218154,
        //
        // Summary:
        //     The target device does not allow debugging this process.
        E_DEBUGGING_BLOCKED_ON_TARGET = -2147218153,
        //
        // Summary:
        //     Unable to debug .NET Native code. Install the Microsoft .NET Native Developer
        //     SDK. Alternatively debug with native code type.
        E_DOTNETNATIVE_SDK_NOT_INSTALLED = -2147218152,
        //
        // Summary:
        //     The operation was canceled.
        COR_E_OPERATIONCANCELED = -2146233029,
        //
        // Summary:
        //     A component dll failed to load. Try to restart this application. If failures
        //     continue, try disabling any installed add-ins or repair your installation.
        E_XAPI_COMPONENT_LOAD_FAILURE = -1898053632,
        //
        // Summary:
        //     Xapi has not been initialized on this thread. Call ComponentManager.InitializeThread.
        E_XAPI_NOT_INITIALIZED = -1898053631,
        //
        // Summary:
        //     Xapi has already been initialized on this thread.
        E_XAPI_ALREADY_INITIALIZED = -1898053630,
        //
        // Summary:
        //     Xapi event thread aborted unexpectedly.
        E_XAPI_THREAD_ABORTED = -1898053629,
        //
        // Summary:
        //     Component failed a call to QueryInterface. QueryInterface implementation or component
        //     configuration is incorrect.
        E_XAPI_BAD_QUERY_INTERFACE = -1898053628,
        //
        // Summary:
        //     Object requested which is not available at the caller's component level.
        E_XAPI_UNAVAILABLE_OBJECT = -1898053627,
        //
        // Summary:
        //     Failed to process configuration file. Try to restart this application. If failures
        //     continue, try to repair your installation.
        E_XAPI_BAD_CONFIG = -1898053626,
        //
        // Summary:
        //     Failed to initialize managed/native marshalling system. Try to restart this application.
        //     If failures continue, try to repair your installation.
        E_XAPI_MANAGED_DISPATCHER_CONNECT_FAILURE = -1898053625,
        //
        // Summary:
        //     This operation may only be preformed while processing the object's 'Create' event.
        E_XAPI_DURING_CREATE_EVENT_REQUIRED = -1898053624,
        //
        // Summary:
        //     This operation may only be preformed by the component which created the object.
        E_XAPI_CREATOR_REQUIRED = -1898053623,
        //
        // Summary:
        //     The work item cannot be appended to the work list because it is already complete.
        E_XAPI_WORK_LIST_COMPLETE = -1898053622,
        //
        // Summary:
        //     'Execute' may not be called on a work list which has already started.
        E_XAPI_WORKLIST_ALREADY_STARTED = -1898053621,
        //
        // Summary:
        //     The interface implementation released the completion routine without calling
        //     it.
        E_XAPI_COMPLETION_ROUTINE_RELEASED = -1898053620,
        //
        // Summary:
        //     Operation is not supported on this thread.
        E_XAPI_WRONG_THREAD = -1898053619,
        //
        // Summary:
        //     No component with the given component id could be found in the configuration
        //     store.
        E_XAPI_COMPONENTID_NOT_FOUND = -1898053618,
        //
        // Summary:
        //     Call was attempted to a remote connection from a server-side component (component
        //     level > 100000). This is not allowed.
        E_XAPI_WRONG_CONNECTION_OBJECT = -1898053617,
        //
        // Summary:
        //     Destination of this call is on a remote connection and this method doesn't support
        //     remoting.
        E_XAPI_METHOD_NOT_REMOTED = -1898053616,
        //
        // Summary:
        //     The network connection to the Visual Studio Remote Debugger was lost.
        E_XAPI_REMOTE_DISCONNECTED = -1898053615,
        //
        // Summary:
        //     The network connection to the Visual Studio Remote Debugger has been closed.
        E_XAPI_REMOTE_CLOSED = -1898053614,
        //
        // Summary:
        //     A protocol compatibility error occurred between Visual Studio and the Remote
        //     Debugger. Please ensure that the Visual Studio and Remote debugger versions match.
        E_XAPI_INCOMPATIBLE_PROTOCOL = -1898053613,
        //
        // Summary:
        //     Maximum allocation size exceeded while processing a remoting message.
        E_XAPI_MAX_PACKET_EXCEEDED = -1898053612,
        //
        // Summary:
        //     An object already exists with the same key value.
        E_XAPI_OBJECT_ALREADY_EXISTS = -1898053611,
        //
        // Summary:
        //     An object cannot be found with the given key value.
        E_XAPI_OBJECT_NOT_FOUND = -1898053610,
        //
        // Summary:
        //     A data item already exists with the same key value.
        E_XAPI_DATA_ITEM_ALREADY_EXISTS = -1898053609,
        //
        // Summary:
        //     A data item cannot be for this component found with the given data item ID.
        E_XAPI_DATA_ITEM_NOT_FOUND = -1898053608,
        //
        // Summary:
        //     Interface implementation failed to provide a required out param.
        E_XAPI_NULL_OUT_PARAM = -1898053607,
        //
        // Summary:
        //     Strong name signature validation error while trying to load the managed dispatcher
        E_XAPI_MANAGED_DISPATCHER_SIGNATURE_ERROR = -1898053600,
        //
        // Summary:
        //     Method may only be called by components which load in the IDE process (component
        //     level > 100000).
        E_XAPI_CLIENT_ONLY_METHOD = -1898053599,
        //
        // Summary:
        //     Method may only be called by components which load in the remote debugger process
        //     (component level < 100000).
        E_XAPI_SERVER_ONLY_METHOD = -1898053598,
        //
        // Summary:
        //     A component dll could not be found. If failures continue, try disabling any installed
        //     add-ins or repairing your installation.
        E_XAPI_COMPONENT_DLL_NOT_FOUND = -1898053597,
        //
        // Summary:
        //     Operation requires the remote debugger be updated to a newer version.
        E_XAPI_REMOTE_NEW_VER_REQUIRED = -1898053596,
        //
        // Summary:
        //     Symbols are not loaded for the target dll.
        E_SYMBOLS_NOT_LOADED = -1842151424,
        //
        // Summary:
        //     Symbols for the target dll do not contain source information.
        E_SYMBOLS_STRIPPED = -1842151423,
        //
        // Summary:
        //     Breakpoint could not be written at the specified instruction address.
        E_BP_INVALID_ADDRESS = -1842151422,
        //
        // Summary:
        //     Breakpoints cannot be set in optimized code when the debugger option 'Just My
        //     Code' is enabled.
        E_BP_IN_OPTIMIZED_CODE = -1842151420,
        //
        // Summary:
        //     The Common Language Runtime was unable to set the breakpoint.
        E_BP_CLR_ERROR = -1842151418,
        //
        // Summary:
        //     Cannot set breakpoints in .NET Framework methods which are implemented in native
        //     code (ex: 'extern' function).
        E_BP_CLR_EXTERN_FUNCTION = -1842151417,
        //
        // Summary:
        //     Cannot set breakpoint, target module is currently unloaded.
        E_BP_MODULE_UNLOADED = -1842151416,
        //
        // Summary:
        //     Stopping events cannot be sent. See stopping event processing documentation for
        //     more information.
        E_STOPPING_EVENT_REJECTED = -1842151415,
        //
        // Summary:
        //     This operation is not permitted because the target process is already stopped.
        E_TARGET_ALREADY_STOPPED = -1842151414,
        //
        // Summary:
        //     This operation is not permitted because the target process is not stopped.
        E_TARGET_NOT_STOPPED = -1842151413,
        //
        // Summary:
        //     This operation is not allowed on this thread.
        E_WRONG_THREAD = -1842151412,
        //
        // Summary:
        //     This operation is not allowed at this time.
        E_WRONG_TIME = -1842151411,
        //
        // Summary:
        //     The caller is not allowed to request this operation. This operation must be requested
        //     by a different component.
        E_WRONG_COMPONENT = -1842151410,
        //
        // Summary:
        //     Operation is only permitted on the latest version of an edited method.
        E_WRONG_METHOD_VERSION = -1842151409,
        //
        // Summary:
        //     A memory read or write operation failed because the specified memory address
        //     is not currently valid.
        E_INVALID_MEMORY_ADDRESS = -1842151408,
        //
        // Summary:
        //     No source information is available for this instruction.
        E_INSTRUCTION_NO_SOURCE = -1842151407,
        //
        // Summary:
        //     Failed to load localizable resource from vsdebugeng.impl.resources.dll. If this
        //     problem persists, please repair your Visual Studio installation via 'Add or Remove
        //     Programs' in Control Panel.
        E_VSDEBUGENG_RESOURCE_LOAD_FAILURE = -1842151406,
        //
        // Summary:
        //     DkmVariant is of a form that marshalling is not supported. Marshalling is supported
        //     for primitives types, strings, and safe arrays of primitives.
        E_UNMARSHALLABLE_VARIANT = -1842151405,
        //
        // Summary:
        //     An incorrect version of vsdebugeng.dll was loaded into Visual Studio. Please
        //     repair your Visual Studio installation.
        E_VSDEBUGENG_DEPLOYMENT_ERROR = -1842151404,
        //
        // Summary:
        //     The remote debugger was unable to initialize Microsoft Windows Web Services (webservices.dll).
        //     If the problem continues, try reinstalling the Windows Web Services redistributable.
        //     This redistributable can be found under the 'Remote Debugger\Common Resources\Windows
        //     Updates' folder.
        E_WEBSERVICES_LOAD_FAILURE = -1842151403,
        //
        // Summary:
        //     Visual Studio encountered an error while loading a Windows component (Global
        //     Interface Table). If the problem persists, this may be an indication of operating
        //     system corruption, and Windows may need to be reinstalled.
        E_GLOBAL_INTERFACE_POINTER_FAILURE = -1842151402,
        //
        // Summary:
        //     Windows authentication was unable to establish a secure connection to the remote
        //     computer.
        E_REMOTE_AUTHENTICATION_ERROR = -1842151401,
        //
        // Summary:
        //     The Remote Debugger was unable to locate a resource dll (vsdebugeng.impl.resources.dll).
        //     Please ensure that the complete remote debugger folder was copied or installed
        //     on the target computer.
        E_CANNOT_FIND_REMOTE_RESOURCES = -1842151400,
        //
        // Summary:
        //     The hardware does not support monitoring the requested number of bytes.
        E_INVALID_DATABP_SIZE = -1842151392,
        //
        // Summary:
        //     The maximum number of data breakpoints have already been set.
        E_INVALID_DATABP_ALLREGSUSED = -1842151391,
        //
        // Summary:
        //     Breakpoints cannot be set while debugging a minidump.
        E_DUMPS_DO_NOT_SUPPORT_BREAKPOINTS = -1842151390,
        //
        // Summary:
        //     The minidump is from an ARM-based computer and can only be debugged on an ARM
        //     computer.
        E_DUMP_ARM_ARCHITECTURE = -1842151389,
        //
        // Summary:
        //     The minidump is from an unknown processor, and cannot be debugged with this version
        //     of Visual Studio.
        E_DUMP_UNKNOWN_ARCHITECTURE = -1842151388,
        //
        // Summary:
        //     The shell failed to find a checksum for this file.
        E_NO_CHECKSUM = -1842151387,
        //
        // Summary:
        //     On x64, context control must be included in a SetThreadContext
        E_CONTEXT_CONTROL_REQUIRED = -1842151386,
        //
        // Summary:
        //     The size of the buffer does not match the size of the register.
        E_INVALID_REGISTER_SIZE = -1842151385,
        //
        // Summary:
        //     The requested register was not found in the stack frame's unwound register collection.
        E_REGISTER_NOT_FOUND = -1842151384,
        //
        // Summary:
        //     Cannot set a read-only register.
        E_REGISTER_READONLY = -1842151383,
        //
        // Summary:
        //     Cannot set a register in a frame that is not the top of the stack.
        E_REG_NOT_TOP_STACK = -1842151376,
        //
        // Summary:
        //     String could not be read within the specified maximum number of characters.
        E_STRING_TOO_LONG = -1842151375,
        //
        // Summary:
        //     The memory region does not meet the requested protection flags.
        E_INVALID_MEMORY_PROTECT = -1842151374,
        //
        // Summary:
        //     Instruction is invalid or unknown to the disassembler.
        E_UNKNOWN_CPU_INSTRUCTION = -1842151373,
        //
        // Summary:
        //     An invalid runtime was specified for this operation.
        E_INVALID_RUNTIME = -1842151372,
        //
        // Summary:
        //     Variable is optimized away.
        E_VARIABLE_OPTIMIZED_AWAY = -1842151371,
        //
        // Summary:
        //     The text span is not currently loaded in the specified script document.
        E_TEXT_SPAN_NOT_LOADED = -1842151370,
        //
        // Summary:
        //     This location could not be mapped to client side script.
        E_SCRIPT_SPAN_MAPPING_FAILED = -1842151369,
        //
        // Summary:
        //     The file requested must be less than 100 megabytes in size
        E_DEPLOY_FILE_TOO_LARGE = -1842151368,
        //
        // Summary:
        //     The file path requested could not be written to as it is invalid. Ensure the
        //     path does not contain a file where a directory is expected.
        E_DEPLOY_FILE_PATH_INVALID = -1842151367,
        //
        // Summary:
        //     Script debugging is not enabled for WWAHost.exe.
        E_SCRIPT_DEBUGGING_DISABLED_WWAHOST_ATTACH_FAILED = -1842151360,
        //
        // Summary:
        //     The file path requested for deletion does not exist.
        E_DEPLOY_FILE_NOT_EXIST = -1842151359,
        //
        // Summary:
        //     A command is already executing, only one may execute at a time. Please wait for
        //     the executable to exit, or abort the command.
        E_EXECUTE_COMMAND_IN_PROGRESS = -1842151358,
        //
        // Summary:
        //     The specified file path is a relative or unknown path format. File paths must
        //     be fully qualified.
        E_INVALID_FULL_PATH = -1842151357,
        //
        // Summary:
        //     Windows Store app debugging is not possible when the remote debugger is running
        //     as a service. Run the Remote Debugger Configuration Wizard on the target computer,
        //     and uncheck the option to start the remote debugger service. Then start the Visual
        //     Studio Remote Debugging Monitor application.
        E_CANNOT_DEBUG_APP_PACKAGE_IN_RDBSERVICE = -1842151356,
        //
        // Summary:
        //     Applications cannot be launched under the debugger when the remote debugger is
        //     running as a service. Run the Remote Debugger Configuration Wizard on the target
        //     computer, and uncheck the option to start the remote debugger service. Then start
        //     the Visual Studio Remote Debugging Monitor application.
        E_CANNOT_LAUNCH_IN_RDBSERVICE = -1842151355,
        //
        // Summary:
        //     The AD7 AL Causality bridge has already been initialized.
        E_CAUSALITY_BRIDGE_ALREADY_INITIALIZED = -1842151354,
        //
        // Summary:
        //     App Packages may only be shutdown as part of a Visual Studio build operation.
        E_DEPLOY_APPX_SHUTDOWN_WRONG_TIME = -1842151353,
        //
        // Summary:
        //     A Microsoft Windows component is not correctly registered. If the problem persists,
        //     try repairing your Windows installation, or reinstalling Windows.
        E_WINDOWS_REG_ERROR = -1842151352,
        //
        // Summary:
        //     The application never reached a suspended state.
        E_APP_PACKAGE_NEVER_SUSPENDED = -1842151351,
        //
        // Summary:
        //     A different version of this script file has been loaded by the debugged process.
        //     The script file may need to be reloaded.
        E_SCRIPT_FILE_DIFFERENT_CONTENT = -1842151350,
        //
        // Summary:
        //     No stack frame was found.
        E_NO_FRAME = -1842151349,
        //
        // Summary:
        //     Operation is not supported while interop debugging.
        E_NOT_SUPPORTED_INTEROP = -1842151348,
        //
        // Summary:
        //     The selected accelerator does not support the run current tile to cursor operation.
        E_GPU_BARRIER_BREAKPOINT_NOT_SUPPORTED = -1842151347,
        //
        // Summary:
        //     Data breakpoints are not supported on this platform.
        E_DATABPS_NOTSUPPORTED = -1842151346,
        //
        // Summary:
        //     The debugger failed to attach to the process requested in the DkmDebugProcessRequest.
        E_DEBUG_PROCESS_REQUEST_FAILED = -1842151345,
        //
        // Summary:
        //     An invalid NativeOffset or CPUInstructionPart value was used with a DkmClrInstructionAddress
        //     or DkmClrInstructionSymbol
        E_INVALID_CLR_INSTRUCTION_NATIVE_OFFSET = -1842151339,
        //
        // Summary:
        //     Managed heap is not in a state that can be enumerated
        E_MANAGED_HEAP_NOT_ENUMERABLE = -1842151338,
        //
        // Summary:
        //     This operation is unavailable when mixed mode debugging with Script
        E_OPERATION_UNAVAILABLE_SCRIPT_INTEROP = -1842151337,
        //
        // Summary:
        //     This operation is unavailable when debugging native-compiled .NET code.
        E_OPERATION_UNAVAILABLE_CLR_NC = -1842151336,
        //
        // Summary:
        //     Symbol file contains data which is in an unexpected format.
        E_BAD_SYMBOL_DATA = -1842151335,
        //
        // Summary:
        //     Dynamically enabling script debugging in the target process failed.
        E_ENABLE_SCRIPT_DEBUGGING_FAILED = -1842151334,
        //
        // Summary:
        //     Expression evaluation is not available in async call stack frames.
        E_SCRIPT_ASYNC_FRAME_EE_UNAVAILABLE = -1842151333,
        //
        // Summary:
        //     This dump does not contain any thread information or the thread information is
        //     corrupt. Visual Studio does not support debugging of dumps without valid thread
        //     information.
        E_DUMP_NO_THREADS = -1842151332,
        //
        // Summary:
        //     DkmLoadCompleteEventDeferral.Add cannot be called after the load complete event
        //     has been sent.
        E_LOAD_COMPLETE_ALREADY_SENT = -1842151331,
        //
        // Summary:
        //     DkmLoadCompleteEventDeferral was not present in the list during a call to DkmLoadCompleteEventDeferral.Remove.
        E_LOAD_COMPLETE_DEFERRAL_NOT_FOUND = -1842151330,
        //
        // Summary:
        //     The buffer size specified was too large to marshal over the remote boundary.
        E_MARSHALLING_SIZE_TOO_LARGE = -1842151329,
        //
        // Summary:
        //     Emulation of iterator for results view failed. This is typically caused when
        //     the iterator calls into native code.
        E_CANNOT_EMULATE_RESULTS_VIEW = -1842151328,
        //
        // Summary:
        //     Managed heap enumeration is attempted on running target. This is typically caused
        //     by continuing the process while heap enumeration is in progress.
        E_MANAGED_HEAP_ENUMERATION_TARGET_NOT_STOPPED = -1842151327
    }
}
