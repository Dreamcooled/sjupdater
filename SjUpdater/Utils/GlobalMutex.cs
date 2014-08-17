using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SjUpdater.Utils
{
    public static class GlobalMutex
    {

        private static Mutex mutex;
        private static bool hasHandle;

        public static bool TryGetMutex()
        {
            //source: http://stackoverflow.com/questions/229565/what-is-a-good-pattern-for-using-a-global-mutex-in-c

            // get application GUID as defined in AssemblyInfo.cs
            //string appGuid = "SjUpdater\\v" +Assembly.GetExecutingAssembly().GetName().Version.Major;
            string appGuid = ((GuidAttribute)Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(GuidAttribute), false).GetValue(0)).Value.ToString();

            // unique id for global mutex - Global prefix means it is global to the machine
            string mutexId = string.Format("Global\\{{{0}}}", appGuid);

            mutex = new Mutex(false, mutexId);
            
            // edited by Jeremy Wiebe to add example of setting up security for multi-user usage
            // edited by 'Marc' to work also on localized systems (don't use just "Everyone") 
            var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                MutexRights.FullControl, AccessControlType.Allow);
            var securitySettings = new MutexSecurity();
            securitySettings.AddAccessRule(allowEveryoneRule);
            mutex.SetAccessControl(securitySettings);

            // edited by acidzombie24
            try
            {
                try
                {
                    // note, you may want to time out here instead of waiting forever
                    // edited by acidzombie24
                    // mutex.WaitOne(Timeout.Infinite, false);
                    hasHandle = mutex.WaitOne(5000, false);
                    if (hasHandle == false)
                        throw new TimeoutException("Timeout waiting for exclusive access");
                }
                catch (AbandonedMutexException)
                {
                    // Log the fact the mutex was abandoned in another process, it will still get aquired
                    hasHandle = true;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        static public void ReleaseMutex()
        {
            if (hasHandle)
            {
                mutex.ReleaseMutex();
            }
        }
    }
}
