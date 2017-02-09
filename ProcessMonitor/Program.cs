using System;
using System.Diagnostics;
using Microsoft.Diagnostics.Runtime;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace ProcessMonitor
{
    class Program
    {
        internal static class NativeMethods
        {
            // see https://msdn.microsoft.com/en-us/library/windows/desktop/ms684139%28v=vs.85%29.aspx
            public static bool Is64Bit(Process process)
            {
                // if (Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE") == "x86")
                //     return false;

                bool isWow64;
                if (!IsWow64Process(process.Handle, out isWow64))
                    throw new Win32Exception();
                return !isWow64;
            }

            [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool IsWow64Process([In] IntPtr process, [Out] out bool wow64Process);
        }

        static void Main(string[] args)
        {
            while(true)
            {
                Process[] dllhost = Process.GetProcessesByName("dllhost");

                Console.ReadLine();

                foreach (Process process in dllhost)
                {
                    try
                    {
                        if (!NativeMethods.Is64Bit(process))
                        {
                            DataTarget dt = DataTarget.AttachToProcess(process.Id, 5000, AttachFlag.Passive);

                            // Just Native Modules (not managed)
                            // foreach (ModuleInfo md in dt.EnumerateModules())
                            // {
                            //     Console.WriteLine("PID: {0}, Description: {1}", process.Id, md.FileName);
                            // }

                            // Also managed modules
                            // First, loop through each Clr in the process (there may be 
                            // multiple in the side-by-side scenario).
                            foreach (ClrInfo clrVersion in dt.ClrVersions)
                            {
                                ClrRuntime runtime = clrVersion.CreateRuntime();

                                foreach (ClrModule module in runtime.Modules)
                                {
                                    if (module.IsFile)
                                        Console.WriteLine("Process: {0}, Module: {1}, Arq: {2}", process.Id, module.FileName, NativeMethods.Is64Bit(process));
                                }

                                /*
                                 Note that ClrMD builds state in caches with every API call.
                                 Since the process is live and constantly changing this means 
                                 subsequent calls to ClrRuntime.Modules will get the same  
                                 data back over and over. Instead you need to call 
                                 ‘ClrRuntime.Flush’ if your reuse the runtime object to check 
                                 the module list repeatedly:
                                */

                                /*
                                 Uncomment this if you ever loop through ClrRuntime.Modules
                                 again, expecting updated results.
                                */
                                runtime.Flush();
                            }
                        }
                        else
                        {
                            Console.WriteLine("Process: {0}, is x64", process.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Oooops... This broke: {0}", ex.Message);
                    }
                }
            }
        }
    }
}
