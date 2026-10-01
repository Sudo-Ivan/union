using System;
using System.Runtime.InteropServices;
using NLog;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Instrumentation;

namespace NzbDrone.Common.Disk
{
    // Best-effort io scheduling for bulk disk work. On Linux, drops the calling
    // thread to IOPRIO_CLASS_IDLE so scans only get disk time when idle.
    // No-op on other platforms or if the kernel rejects it.
    public static class IoPriority
    {
        private const int IOPRIO_WHO_PROCESS = 1;
        private const int IOPRIO_CLASS_IDLE = 3;
        private const int IOPRIO_CLASS_BE = 2;
        private const int IOPRIO_CLASS_SHIFT = 13;

        private static readonly Logger Logger = NzbDroneLogger.GetLogger(typeof(IoPriority));

        [DllImport("libc", SetLastError = true, EntryPoint = "ioprio_set")]
        private static extern int SetIoPriority(int which, int who, int ioprio);

        public static void Lower()
        {
            if (!OsInfo.IsLinux)
            {
                return;
            }

            try
            {
                if (SetIoPriority(IOPRIO_WHO_PROCESS, 0, IOPRIO_CLASS_IDLE << IOPRIO_CLASS_SHIFT) != 0)
                {
                    Logger.Trace("ioprio_set idle failed: {0}", Marshal.GetLastWin32Error());
                }
            }
            catch (Exception e)
            {
                Logger.Trace(e, "ioprio_set unavailable");
            }
        }

        public static void Restore()
        {
            if (!OsInfo.IsLinux)
            {
                return;
            }

            try
            {
                // data 4 is the default best-effort priority
                SetIoPriority(IOPRIO_WHO_PROCESS, 0, (IOPRIO_CLASS_BE << IOPRIO_CLASS_SHIFT) | 4);
            }
            catch
            {
                // ignore
            }
        }
    }
}
