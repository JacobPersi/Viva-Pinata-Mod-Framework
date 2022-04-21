using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace PinataParty.Bootstrap {

    public static class Win32 {

        public static int CreateSuspended(string strFileExec, string strArguments = "") {
            Win32.PROCESS_INFORMATION pi = new Win32.PROCESS_INFORMATION();
            Win32.STARTUPINFO si = new Win32.STARTUPINFO();

            try {
                if (strFileExec.Length == 0 || !File.Exists(strFileExec))
                    return 0;

                bool bCreated = Win32.CreateProcess(
                    strFileExec, strArguments,
                    IntPtr.Zero, IntPtr.Zero,
                    false,
                    Win32.NORMAL_PRIORITY_CLASS | Win32.CREATE_SUSPENDED,
                    IntPtr.Zero,
                    Path.GetDirectoryName(strFileExec),
                    ref si, out pi
                    );

                return pi.dwProcessId;
            }
            catch {
                return 0;
            }
            finally {
                if (pi.hProcess != IntPtr.Zero)
                    Win32.CloseHandle(pi.hProcess);
                if (pi.hThread != IntPtr.Zero)
                    Win32.CloseHandle(pi.hThread);
            }
        }

        public static bool InjectModule(int dwProcessId, string strModulePath, bool bSuspendProcess = true) {
            if (!IsRunning(dwProcessId))
                return false;

            if (strModulePath.Length == 0 || !File.Exists(strModulePath))
                return false;

            IntPtr lpKernel32 = IntPtr.Zero; // Kernel32.dll module address.
            IntPtr lpLoadLibA = IntPtr.Zero; // LoadLibraryA procedure address.
            IntPtr lpAllocPtr = IntPtr.Zero; // Allocated memory pointer.
            IntPtr lpRmThread = IntPtr.Zero; // Remote thread handle.
            Process proc = null; // Process instance.

            try {
                if (bSuspendProcess)
                    SuspendProcess(dwProcessId);

                // Obtain kernel32.dll module address.
                lpKernel32 = Win32.GetModuleHandle("kernel32.dll");
                if (lpKernel32 == IntPtr.Zero)
                    throw new Exception("[InjectModule] Error: Failed to obtain kernel32.dll module address.");

                // Obtain procedure address for LoadLibraryA.
                lpLoadLibA = Win32.GetProcAddress(lpKernel32, "LoadLibraryA");
                if (lpLoadLibA == IntPtr.Zero)
                    throw new Exception("[InjectModule] Error: Failed to obtain procedure address for LoadLibraryA.");

                // Obtain process instance.
                proc = Process.GetProcessById(dwProcessId);
                if (proc == null)
                    throw new Exception("Process specified is not running.");

                // Allocate memory in remote process.
                lpAllocPtr = Win32.VirtualAllocEx(proc.Handle, IntPtr.Zero, (uint)strModulePath.Length, Win32.AllocationType.Commit, Win32.MemoryProtection.ExecuteReadWrite);
                if (lpAllocPtr == IntPtr.Zero)
                    throw new Exception("[InjectModule] Error: Failed to allocate memory in remote process.");

                // Attempt to write module path to allocated memory.
                int nNumBytesWritten = 0;
                byte[] btModulePath = Encoding.ASCII.GetBytes(strModulePath);
                bool bWasWritten = Win32.WriteProcessMemory(proc.Handle, lpAllocPtr, btModulePath, (uint)btModulePath.Length, out nNumBytesWritten);
                if (!bWasWritten || nNumBytesWritten == 0 || (nNumBytesWritten < btModulePath.Length))
                    throw new Exception("[InjectModule] Error: Failed to write module path to remote memory.");

                // Create remote thread calling LoadLibraryA.
                lpRmThread = Win32.CreateRemoteThread(proc.Handle, IntPtr.Zero, 0, lpLoadLibA, lpAllocPtr, 0, IntPtr.Zero);
                if (lpRmThread == IntPtr.Zero)
                    throw new Exception("[InjectModule] Error: Failed to create remote thread.");

                // Obtain thread exit code.
                uint nExitCode = 0;
                Win32.WaitForSingleObject(lpRmThread, Win32.INFINITE);
                Win32.GetExitCodeThread(lpRmThread, out nExitCode);

                if (nExitCode == 0)
                    throw new Exception("[InjectModule] Error: Thread did not return valid address for loaded module.");

                return true;
            }
            catch {
                return false;
            }
            finally {
                if (lpRmThread != IntPtr.Zero)
                    Win32.CloseHandle(lpRmThread);
                if (lpAllocPtr != IntPtr.Zero && proc != null)
                    Win32.VirtualFreeEx(proc.Handle, lpAllocPtr, (uint)strModulePath.Length, Win32.AllocationType.Decommit);

                if (bSuspendProcess)
                    ResumeProcess(dwProcessId);
            }
        }

        public static bool CallExport(int dwProcessId, string strModuleName, string strExportName) {
            return CallExport(dwProcessId, strModuleName, strExportName);
        }

        public static bool CallExport(int dwProcessId, string strModuleName, string strExportName, uint nExpectedReturn, bool bIgnoreReturn, byte[] btArgument = null) {
            if (!IsRunning(dwProcessId))
                return false;

            if (strModuleName.Length == 0 || !File.Exists(strModuleName))
                return false;

            ProcessModule module = null;
            if (!HasModule(dwProcessId, strModuleName, out module))
                return false;

            IntPtr lpBaseAddr = IntPtr.Zero; // Module base address.
            IntPtr lpProcAddr = IntPtr.Zero; // Export procedure address.
            IntPtr lpAllocPtr = IntPtr.Zero; // Allocated memory pointer.
            IntPtr lpRmThread = IntPtr.Zero; // Remote thread handle.
            Process proc = null; // Process instance.

            try {
                // Load module into current process.
                lpBaseAddr = Win32.LoadLibrary(strModuleName);
                if (lpBaseAddr == IntPtr.Zero)
                    throw new Exception("[CallExport] Error: Failed to load library into current process.");

                // Obtain procedure address for export.
                lpProcAddr = Win32.GetProcAddress(lpBaseAddr, strExportName);
                if (lpProcAddr == IntPtr.Zero)
                    throw new Exception("[CallExport] Error: Failed to obtain exported procedure address.");

                // Calculate offset to export.
                IntPtr lpCalcOffset = new IntPtr(lpProcAddr.ToInt32() - lpBaseAddr.ToInt32());
                if (lpCalcOffset == IntPtr.Zero)
                    throw new Exception("[CallExport] Error: Failed to calculate offset to expot function.");

                // Obtain process instance.
                proc = Process.GetProcessById(dwProcessId);
                if (proc == null)
                    throw new Exception("Process specified is not running.");

                // Handle argument param if passed.
                if (btArgument != null && btArgument.Length > 0) {
                    lpAllocPtr = Win32.VirtualAllocEx(proc.Handle, IntPtr.Zero, (uint)btArgument.Length, Win32.AllocationType.Commit, Win32.MemoryProtection.ExecuteReadWrite);
                    if (lpAllocPtr == IntPtr.Zero)
                        throw new Exception("[CallExport] Error: Failed to allocate memory in remote process.");

                    int nNumBytesWritten = 0;
                    bool bWasWritten = Win32.WriteProcessMemory(proc.Handle, lpAllocPtr, btArgument, (uint)btArgument.Length, out nNumBytesWritten);
                    if (!bWasWritten || nNumBytesWritten == 0 || (nNumBytesWritten < btArgument.Length))
                        throw new Exception("[CallExport] Error: Failed to write argument data to remote process.");
                }

                // Create remote thread calling export.
                lpRmThread = Win32.CreateRemoteThread(proc.Handle, IntPtr.Zero, 0, lpProcAddr,
                    (lpAllocPtr != IntPtr.Zero) ? lpAllocPtr : IntPtr.Zero,
                    0, IntPtr.Zero
                    );
                if (lpRmThread == IntPtr.Zero)
                    throw new Exception("[CallExport] Error: Failed to create remote thread.");

                // Obtain thread exit code.
                uint nExitCode = 0;
                Win32.WaitForSingleObject(lpRmThread, Win32.INFINITE);
                Win32.GetExitCodeThread(lpRmThread, out nExitCode);
                if (bIgnoreReturn == false) {
                    if (nExitCode != nExpectedReturn)
                        throw new Exception("[CallExport] Error: Remote thread did not return expected value.");
                }

                return true;
            }
            catch {
                return false;
            }
            finally {
                if (lpRmThread != IntPtr.Zero)
                    Win32.CloseHandle(lpRmThread);
                if (lpAllocPtr != IntPtr.Zero && proc != null)
                    Win32.VirtualFreeEx(proc.Handle, lpAllocPtr, (uint)btArgument.Length, Win32.AllocationType.Decommit);
            }
        }

        public static bool ResumeProcess(int dwProcessId, bool bForceResume = false) {
            try {
                // Obtain process instance.
                Process proc = Process.GetProcessById(dwProcessId);
                if (proc == null)
                    return false;

                // Obtain thread handle.
                ProcessThread thread = proc.Threads[0];
                IntPtr lpThreadHandle = Win32.OpenThread(Win32.ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                if (lpThreadHandle == IntPtr.Zero)
                    return false;

                if (bForceResume == true)
                    while (Win32.ResumeThread(lpThreadHandle) != 0) ;
                else
                    Win32.ResumeThread(lpThreadHandle);

                Win32.CloseHandle(lpThreadHandle);
                return true;
            }
            catch { return false; }
        }

        public static bool SuspendProcess(int dwProcessId) {
            try {
                // Obtain process instance.
                Process proc = Process.GetProcessById(dwProcessId);
                if (proc == null)
                    return false;

                // Obtain thread handle.
                ProcessThread thread = proc.Threads[0];
                IntPtr lpThreadHandle = Win32.OpenThread(Win32.ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                if (lpThreadHandle == IntPtr.Zero)
                    return false;

                Win32.SuspendThread(lpThreadHandle);
                Win32.CloseHandle(lpThreadHandle);

                return true;
            }
            catch { return false; }
        }

        public static bool KillProcess(int dwProcessId) {
            try {
                Process proc = Process.GetProcessById(dwProcessId);
                if (proc == null)
                    return false;

                proc.Kill();
                return true;
            }
            catch { return false; }
        }

        public static bool HasModule(int dwProcessId, string strModuleName, out ProcessModule module) {
            try {
                // Obtain process instance.
                Process proc = Process.GetProcessById(dwProcessId);
                if (proc == null) {
                    module = null;
                    return false;
                }

                // Locate the module.
                foreach (ProcessModule mod in proc.Modules) {
                    if (mod.ModuleName.ToLower() == strModuleName.ToLower()) {
                        module = mod;
                        return true;
                    }
                }

                module = null;
                return false;
            }
            catch { module = null; return false; }
        }

        public static bool IsRunning(int dwProcessId) {
            try {
                Process proc = Process.GetProcessById(dwProcessId);
                if (proc == null || proc.HasExited == true)
                    return false;
                return true;
            }
            catch { return false; }
        }

        public static bool IsSuspended(int dwProcessId) {
            try {
                // Obtain process instance.
                Process proc = Process.GetProcessById(dwProcessId);
                if (proc == null)
                    return false;
                return (proc.Threads[0].ThreadState == ThreadState.Wait && proc.Threads[0].WaitReason == ThreadWaitReason.Suspended);
            }
            catch { return false; }
        }


        public static bool PatchMemory(int dwProcessId, IntPtr address, byte[] bytes) {
            IntPtr processHandle = IntPtr.Zero;
            uint oldProtect;
            int bytesWritten;

            try {
                processHandle = Win32.OpenProcess((int)PROCESS_ALL_ACCESS, false, dwProcessId);

                Win32.VirtualProtectEx(processHandle, address, (uint)bytes.Length, PAGE_EXECUTE_READWRITE, out oldProtect);
                Win32.WriteProcessMemory(processHandle, address, bytes, (uint)bytes.Length, out bytesWritten);
                Win32.VirtualProtectEx(processHandle, address, (uint)bytes.Length, oldProtect, out oldProtect);

                return (bytesWritten != 0);
            }
            catch {
                return false;
            }
            finally {
                if (processHandle != IntPtr.Zero) {
                    Win32.CloseHandle(processHandle);
                }
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool CloseHandle(
        IntPtr hObject
        );

        [DllImport("kernel32.dll")]
        public static extern bool CreateProcess(string lpApplicationName,
            string lpCommandLine,
            [In] IntPtr lpProcessAttributes,
            [In] IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            [In] ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation
            );

        [DllImport("kernel32.dll")]
        public static extern IntPtr CreateRemoteThread(
            IntPtr hProcess,
            IntPtr lpThreadAttributes,
            uint dwStackSize,
            IntPtr lpStartAddress,
            IntPtr lpParameter,
            uint dwCreationFlags,
            IntPtr lpThreadId
            );

        [DllImport("kernel32.dll")]
        public static extern bool GetExitCodeThread(
            IntPtr hThread,
            out uint lpExitCode
            );

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(
            string lpModuleName
            );

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress(
            IntPtr hModule,
            string procName
            );

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, EntryPoint = "LoadLibraryA", SetLastError = true)]
        public static extern IntPtr LoadLibrary(
            string lpFileName
            );

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenThread(
            ThreadAccess dwDesiredAccess,
            bool bInheritHandle,
            uint dwThreadId
            );

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(
            int dwDesiredAccess,
            bool bInheritHandle,
            int dwProcessId
            );


        [DllImport("kernel32.dll")]
        public static extern uint ResumeThread(
            IntPtr hThread
            );

        [DllImport("kernel32.dll")]
        public static extern uint SuspendThread(
            IntPtr hThread
            );

        [DllImport("kernel32.dll")]
        public static extern bool TerminateThread(
            IntPtr hThread,
            uint dwExitCode
            );

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern IntPtr VirtualAllocEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            uint dwSize,
            AllocationType flAllocationType,
            MemoryProtection flProtect
            );

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern bool VirtualFreeEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            uint dwSize,
            AllocationType flFreeType
            );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern UInt32 WaitForSingleObject(
            IntPtr hHandle,
            UInt32 dwMilliseconds
            );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            uint nSize,
            out int lpNumberOfBytesWritten
            );

        [DllImport("kernel32.dll")]
        private static extern bool VirtualProtectEx(
            IntPtr hProcess,
            IntPtr lpAddress,
            uint dwSize,
            uint flNewProtect,
            out uint lpflOldProtect
            );

        [Flags]
        public enum AllocationType {
            Commit = 0x1000,
            Reserve = 0x2000,
            Decommit = 0x4000,
            Release = 0x8000,
            Reset = 0x80000,
            Physical = 0x400000,
            TopDown = 0x100000,
            WriteWatch = 0x200000,
            LargePages = 0x20000000
        }

        [Flags]
        public enum MemoryProtection {
            Execute = 0x10,
            ExecuteRead = 0x20,
            ExecuteReadWrite = 0x40,
            ExecuteWriteCopy = 0x80,
            NoAccess = 0x01,
            ReadOnly = 0x02,
            ReadWrite = 0x04,
            WriteCopy = 0x08,
            GuardModifierflag = 0x100,
            NoCacheModifierflag = 0x200,
            WriteCombineModifierflag = 0x400
        }

        [Flags]
        public enum ThreadAccess : int {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct STARTUPINFO {
            public Int32 cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public Int32 dwX;
            public Int32 dwY;
            public Int32 dwXSize;
            public Int32 dwYSize;
            public Int32 dwXCountChars;
            public Int32 dwYCountChars;
            public Int32 dwFillAttribute;
            public Int32 dwFlags;
            public Int16 wShowWindow;
            public Int16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SECURITY_ATTRIBUTES {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public int bInheritHandle;
        }


        public const UInt32 PROCESS_ALL_ACCESS = 0x1F0FFF;
        public const UInt32 PAGE_EXECUTE_READWRITE = 0x40;

        public const UInt32 DEBUG_PROCESS = 0x00000001;
        public const UInt32 DEBUG_ONLY_THIS_PROCESS = 0x00000002;
        public const UInt32 CREATE_SUSPENDED = 0x00000004;
        public const UInt32 DETACHED_PROCESS = 0x00000008;
        public const UInt32 CREATE_NEW_CONSOLE = 0x00000010;

        public const UInt32 NORMAL_PRIORITY_CLASS = 0x00000020;
        public const UInt32 IDLE_PRIORITY_CLASS = 0x00000040;
        public const UInt32 HIGH_PRIORITY_CLASS = 0x00000080;
        public const UInt32 REALTIME_PRIORITY_CLASS = 0x00000100;

        public const UInt32 INFINITE = 0xFFFFFFFF;
        public const UInt32 WAIT_ABANDONED = 0x00000080;
        public const UInt32 WAIT_OBJECT_0 = 0x00000000;
        public const UInt32 WAIT_TIMEOUT = 0x00000102;
    }
}
