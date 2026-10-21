using System.Runtime.InteropServices;

namespace Savdonoma.Printing
{
    public class RawPrinterHelper
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private class DOCINFO
        {
            public string pDocName = "Chek";
            public string? pOutputFile = null;
            public string pDataType = "RAW";
        }

        [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool OpenPrinter(string name, out IntPtr h, IntPtr d);
        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool ClosePrinter(IntPtr h);
        [DllImport("winspool.drv", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool StartDocPrinter(IntPtr h, int level, [In] DOCINFO di);
        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool EndDocPrinter(IntPtr h);
        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool StartPagePrinter(IntPtr h);
        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool EndPagePrinter(IntPtr h);
        [DllImport("winspool.drv", SetLastError = true)]
        private static extern bool WritePrinter(IntPtr h, byte[] buf, int count, out int written);

        public static bool SendBytes(string printerName, byte[] bytes)
        {
            if (!OpenPrinter(printerName, out var h, IntPtr.Zero)) 
                return false;
            try
            {
                if (!StartDocPrinter(h, 1, new DOCINFO())) 
                    return false;
                StartPagePrinter(h);
                var ok = WritePrinter(h, bytes, bytes.Length, out _);
                EndPagePrinter(h);
                EndDocPrinter(h);
                return ok;
            }
            finally { ClosePrinter(h); }
        }
    }
}
