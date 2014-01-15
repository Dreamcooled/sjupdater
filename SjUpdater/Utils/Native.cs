using System.Runtime.InteropServices;

namespace SjUpdater.Utils
{
    static public class Native
    {
        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        static public extern int memcmp(byte[] b1, byte[] b2, long count);
    }
}
