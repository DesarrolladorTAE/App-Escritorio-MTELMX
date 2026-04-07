using System;
using System.Runtime.InteropServices;

namespace MiTiendaEnLineaMX
{
    public static class RawPrinterHelper
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public class DOCINFOW
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pDocName = "ESC/POS Ticket";

            [MarshalAs(UnmanagedType.LPWStr)]
            public string? pOutputFile;

            [MarshalAs(UnmanagedType.LPWStr)]
            public string pDataType = "RAW";
        }

        [DllImport("winspool.Drv", EntryPoint = "OpenPrinterW", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool OpenPrinter(string szPrinter, out IntPtr hPrinter, IntPtr pd);

        [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterW", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] DOCINFOW di);

        [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true)]
        private static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true)]
        private static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true)]
        private static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true)]
        private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        public static void SendBytesToPrinter(string printerName, byte[] bytes)
        {
            if (string.IsNullOrWhiteSpace(printerName))
                throw new Exception("No se seleccionó impresora USB.");

            if (bytes == null || bytes.Length == 0)
                throw new Exception("No hay bytes para imprimir.");

            if (!OpenPrinter(printerName, out IntPtr hPrinter, IntPtr.Zero))
                throw new Exception("No se pudo abrir la impresora. Error: " + Marshal.GetLastWin32Error());

            try
            {
                var docInfo = new DOCINFOW();

                if (!StartDocPrinter(hPrinter, 1, docInfo))
                    throw new Exception("No se pudo iniciar documento. Error: " + Marshal.GetLastWin32Error());

                try
                {
                    if (!StartPagePrinter(hPrinter))
                        throw new Exception("No se pudo iniciar página. Error: " + Marshal.GetLastWin32Error());

                    IntPtr unmanagedBytes = Marshal.AllocCoTaskMem(bytes.Length);

                    try
                    {
                        Marshal.Copy(bytes, 0, unmanagedBytes, bytes.Length);

                        if (!WritePrinter(hPrinter, unmanagedBytes, bytes.Length, out int written))
                            throw new Exception("No se pudo escribir a la impresora. Error: " + Marshal.GetLastWin32Error());

                        if (written != bytes.Length)
                            throw new Exception($"Solo se escribieron {written} de {bytes.Length} bytes.");
                    }
                    finally
                    {
                        Marshal.FreeCoTaskMem(unmanagedBytes);
                    }

                    if (!EndPagePrinter(hPrinter))
                        throw new Exception("No se pudo finalizar la página. Error: " + Marshal.GetLastWin32Error());
                }
                finally
                {
                    if (!EndDocPrinter(hPrinter))
                        throw new Exception("No se pudo finalizar el documento. Error: " + Marshal.GetLastWin32Error());
                }
            }
            finally
            {
                ClosePrinter(hPrinter);
            }
        }
    }
}
