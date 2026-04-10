using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MiTiendaEnLineaMX
{
    public class PrinterService
    {
        private readonly string _configPath;

        public PrinterService()
        {
            string appDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MiTiendaEnLineaMX"
            );

            Directory.CreateDirectory(appDir);
            _configPath = Path.Combine(appDir, "printer_config.json");
        }

        public async Task PrintAsync(PrinterConfig config, byte[] bytes)
        {
            switch (config.Mode)
            {
                case PrinterMode.Tcp:
                    await PrintTcp(config.TcpHost, config.TcpPort, bytes);
                    break;

                case PrinterMode.Usb:
                    PrintUsb(config.PrinterName, bytes);
                    break;

                default:
                    throw new Exception("Modo de impresión no soportado.");
            }
        }

        public async Task PrintTcp(string ip, int port, byte[] bytes)
        {
            if (string.IsNullOrWhiteSpace(ip))
                throw new Exception("IP vacía.");

            using TcpClient client = new TcpClient();
            await client.ConnectAsync(ip, port);

            using NetworkStream stream = client.GetStream();
            await stream.WriteAsync(bytes, 0, bytes.Length);
            await stream.FlushAsync();
        }

        public void PrintUsb(string printerName, byte[] bytes)
        {
            if (string.IsNullOrWhiteSpace(printerName))
                throw new Exception("Nombre de impresora USB vacío.");

            RawPrinterHelper.SendBytesToPrinter(printerName, bytes);
        }

        public async Task<bool> TestTcpAsync(string ip, int port)
        {
            using TcpClient client = new TcpClient();
            await client.ConnectAsync(ip, port);
            return true;
        }

        public string[] GetInstalledPrinters()
        {
            List<string> printers = new List<string>();

            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                printers.Add(printer);
            }

            return printers.ToArray();
        }

        public async Task SaveConfigAsync(PrinterConfig config)
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_configPath, json, Encoding.UTF8);
        }

        public async Task<PrinterConfig?> LoadConfigAsync()
        {
            if (!File.Exists(_configPath))
                return null;

            string json = await File.ReadAllTextAsync(_configPath, Encoding.UTF8);
            return JsonSerializer.Deserialize<PrinterConfig>(json);
        }

        public byte[] BuildDemoTicket(
            PaperSize paperSize,
            bool cut = true,
            bool openDrawer = false,
            string? qrText = "https://mitiendaenlineamx.com.mx",
            string? logoBase64 = null)
        {
            int cols = paperSize == PaperSize.Mm80 ? 48 : 32;
            int paperWidthDots = paperSize == PaperSize.Mm58 ? 384 : 576;

            List<byte> bytes = new List<byte>();

            bytes.AddRange(Init());
            bytes.AddRange(SetLeftMargin(0));
            bytes.AddRange(SetPrintAreaWidth(paperWidthDots));

            if (!string.IsNullOrWhiteSpace(logoBase64))
            {
                int demoLogoWidth = paperSize == PaperSize.Mm58 ? 256 : 384;
                byte[]? logoBytes = TryBuildImageFromBase64(
                    logoBase64,
                    demoLogoWidth,
                    paperWidthDots,
                    true
                );

                if (logoBytes != null)
                {
                    bytes.AddRange(SetLeftMargin(0));
                    bytes.AddRange(SetPrintAreaWidth(paperWidthDots));
                    bytes.AddRange(AlignCenter());
                    bytes.AddRange(logoBytes);
                    bytes.AddRange(Lf(1));
                }
            }

            bytes.AddRange(SetLeftMargin(0));
            bytes.AddRange(SetPrintAreaWidth(paperWidthDots));
            bytes.AddRange(AlignCenter());
            bytes.AddRange(Bold(true));
            bytes.AddRange(Text("MITIENDAENLINEAMX\n"));
            bytes.AddRange(Bold(false));

            bytes.AddRange(Text("TICKET DE PRUEBA\n"));
            bytes.AddRange(Lf(1));

            bytes.AddRange(AlignLeft());
            bytes.AddRange(Text(Hr(cols)));
            bytes.AddRange(Text(LineLR("Producto A", "$50.00", cols)));
            bytes.AddRange(Text(LineLR("Producto B", "$70.00", cols)));
            bytes.AddRange(Text(Hr(cols)));

            bytes.AddRange(Bold(true));
            bytes.AddRange(Text(LineLR("TOTAL", "$120.00", cols)));
            bytes.AddRange(Bold(false));

            bytes.AddRange(Lf(1));
            bytes.AddRange(AlignCenter());
            bytes.AddRange(Text("Gracias por su compra\n"));

            if (!string.IsNullOrWhiteSpace(qrText))
            {
                bytes.AddRange(SetLeftMargin(0));
                bytes.AddRange(SetPrintAreaWidth(paperWidthDots));
                bytes.AddRange(AlignCenter());
                bytes.AddRange(Lf(1));
                bytes.AddRange(Qr(qrText, 8, 49));
                bytes.AddRange(Lf(1));
            }

            bytes.AddRange(Lf(3));

            if (openDrawer)
            {
                bytes.AddRange(OpenDrawerSequence());
                bytes.AddRange(Lf(1));
            }

            if (cut)
                bytes.AddRange(CutPartial());

            return bytes.ToArray();
        }

        public byte[] BuildTicketFromPayload(PrintPayload payload)
        {
            if (payload == null)
                throw new Exception("Payload vacío.");

            int paperSize = NormalizePaperSize(payload.Paper?.Size ?? 80);
            int paperWidthDots = GetPaperWidthDots(paperSize);

            NormalizeCharsPerLine(
                payload.Paper?.CharsPerLine ?? (paperSize == 58 ? 32 : 48),
                paperSize
            );

            List<byte> bytes = new List<byte>();
            bytes.AddRange(Init());

            bytes.AddRange(SetLeftMargin(0));
            bytes.AddRange(SetPrintAreaWidth(paperWidthDots));

            // LOGO
            if (!string.IsNullOrWhiteSpace(payload.Logo))
            {
                bool center = string.Equals(
                    payload.LogoAlign,
                    "center",
                    StringComparison.OrdinalIgnoreCase
                );

                int logoMaxWidth = NormalizeLogoMaxWidth(payload.LogoMaxWidth, paperSize);

                byte[]? logoBytes = TryBuildImageFromBase64(
                    payload.Logo,
                    logoMaxWidth,
                    paperWidthDots,
                    center
                );

                if (logoBytes != null)
                {
                    bytes.AddRange(SetLeftMargin(0));
                    bytes.AddRange(SetPrintAreaWidth(paperWidthDots));
                    bytes.AddRange(center ? AlignCenter() : AlignLeft());
                    bytes.AddRange(logoBytes);
                    bytes.AddRange(Lf(1));
                }
            }

            bytes.AddRange(SetLeftMargin(0));
            bytes.AddRange(SetPrintAreaWidth(paperWidthDots));
            bytes.AddRange(AlignLeft());

            // TEXTO ANTES DE QR
            if (!string.IsNullOrWhiteSpace(payload.TextBeforeQr))
            {
                bytes.AddRange(Text(NormalizeNewlines(payload.TextBeforeQr)));
                bytes.AddRange(Lf(1));
            }

            // MULTI QR NUEVO
            if (payload.Qrs != null && payload.Qrs.Count > 0)
            {
                foreach (var qr in payload.Qrs)
                {
                    if (qr == null || string.IsNullOrWhiteSpace(qr.Text))
                        continue;

                    bool center = string.Equals(
                        qr.Align,
                        "center",
                        StringComparison.OrdinalIgnoreCase
                    );

                    int qrSize = NormalizeQrSize(qr.Size);
                    int qrEcc = NormalizeQrEcc(qr.Ecc);

                    bytes.AddRange(SetLeftMargin(0));
                    bytes.AddRange(SetPrintAreaWidth(paperWidthDots));
                    bytes.AddRange(center ? AlignCenter() : AlignLeft());
                    bytes.AddRange(Qr(qr.Text, qrSize, qrEcc));
                    bytes.AddRange(Lf(1));

                    if (!string.IsNullOrWhiteSpace(qr.Caption))
                    {
                        bytes.AddRange(SetLeftMargin(0));
                        bytes.AddRange(SetPrintAreaWidth(paperWidthDots));
                        bytes.AddRange(center ? AlignCenter() : AlignLeft());
                        bytes.AddRange(Text(NormalizeNewlines(qr.Caption)));
                        bytes.AddRange(Lf(1));
                    }

                    bytes.AddRange(Lf(1));
                }

                bytes.AddRange(SetLeftMargin(0));
                bytes.AddRange(SetPrintAreaWidth(paperWidthDots));
                bytes.AddRange(AlignLeft());
            }
            // FALLBACK VIEJO
            else if (!string.IsNullOrWhiteSpace(payload.QrText))
            {
                int qrSize = NormalizeQrSize(payload.QrSize);
                int qrEcc = NormalizeQrEcc(payload.QrEcc);

                bytes.AddRange(SetLeftMargin(0));
                bytes.AddRange(SetPrintAreaWidth(paperWidthDots));
                bytes.AddRange(AlignCenter());
                bytes.AddRange(Qr(payload.QrText, qrSize, qrEcc));
                bytes.AddRange(Lf(1));
                bytes.AddRange(AlignLeft());
            }

            // TEXTO DESPUÉS DE QR
            if (!string.IsNullOrWhiteSpace(payload.TextAfterQr))
            {
                bytes.AddRange(SetLeftMargin(0));
                bytes.AddRange(SetPrintAreaWidth(paperWidthDots));
                bytes.AddRange(AlignLeft());
                bytes.AddRange(Text(NormalizeNewlines(payload.TextAfterQr)));
                bytes.AddRange(Lf(1));
            }

            bytes.AddRange(Lf(3));

            // CAJÓN
            if (payload.OpenDrawer)
            {
                bytes.AddRange(OpenDrawerSequence(payload.DrawerPin));
                bytes.AddRange(Lf(1));
            }

            // CORTE
            if (payload.Cut)
                bytes.AddRange(CutPartial());

            return bytes.ToArray();
        }

        public async Task PrintPayloadAsync(PrintPayload payload, PrinterConfig? fallbackConfig = null)
        {
            if (payload == null)
                throw new Exception("Payload vacío.");

            byte[] bytes = BuildTicketFromPayload(payload);

            string transport = (payload.Transport ?? "usb").Trim().ToLowerInvariant();

            if (transport == "tcp")
            {
                string? ip = payload.Printer?.Ip;
                int port = payload.Printer?.Port ?? 9100;

                if (!string.IsNullOrWhiteSpace(ip))
                {
                    await PrintTcp(ip, port, bytes);
                    return;
                }

                if (fallbackConfig != null && fallbackConfig.Mode == PrinterMode.Tcp)
                {
                    await PrintAsync(fallbackConfig, bytes);
                    return;
                }

                throw new Exception("El payload indicó TCP pero no trae IP válida.");
            }

            if (transport == "usb")
            {
                if (fallbackConfig != null && fallbackConfig.Mode == PrinterMode.Usb)
                {
                    await PrintAsync(fallbackConfig, bytes);
                    return;
                }

                if (fallbackConfig != null && !string.IsNullOrWhiteSpace(fallbackConfig.PrinterName))
                {
                    PrintUsb(fallbackConfig.PrinterName, bytes);
                    return;
                }

                throw new Exception("El payload indicó USB pero no hay impresora USB configurada.");
            }

            if (fallbackConfig != null)
            {
                await PrintAsync(fallbackConfig, bytes);
                return;
            }

            throw new Exception($"Transport no soportado: {payload.Transport}");
        }

        private string NormalizeNewlines(string input)
        {
            return input.Replace("\r\n", "\n").Replace("\r", "\n");
        }

        private int NormalizePaperSize(int size)
        {
            return size == 58 ? 58 : 80;
        }

        private int NormalizeCharsPerLine(int charsPerLine, int paperSize)
        {
            int defaultValue = paperSize == 58 ? 32 : 48;

            if (charsPerLine < 16 || charsPerLine > 64)
                return defaultValue;

            return charsPerLine;
        }

        private int NormalizeQrSize(int size)
        {
            if (size < 1) return 1;
            if (size > 16) return 16;
            return size;
        }

        private int NormalizeQrEcc(int ecc)
        {
            if (ecc < 48) return 48;
            if (ecc > 51) return 51;
            return ecc;
        }

        private int NormalizeLogoMaxWidth(int logoMaxWidth, int paperSize)
        {
            int defaultValue = paperSize == 58 ? 256 : 384;

            if (logoMaxWidth < 64 || logoMaxWidth > 1024)
                return defaultValue;

            return logoMaxWidth;
        }

        private int GetPaperWidthDots(int paperSize)
        {
            return paperSize == 58 ? 384 : 576;
        }

        private byte[] SetLeftMargin(int dots)
        {
            if (dots < 0) dots = 0;

            byte nL = (byte)(dots & 0xFF);
            byte nH = (byte)((dots >> 8) & 0xFF);

            return new byte[] { 0x1D, 0x4C, nL, nH };
        }

        private byte[] SetPrintAreaWidth(int dots)
        {
            if (dots < 1) dots = 1;

            byte nL = (byte)(dots & 0xFF);
            byte nH = (byte)((dots >> 8) & 0xFF);

            return new byte[] { 0x1D, 0x57, nL, nH };
        }

        private byte[] Init() => new byte[] { 0x1B, 0x40 };

        private byte[] Lf(int lines = 1)
        {
            if (lines < 1) lines = 1;

            byte[] data = new byte[lines];
            for (int i = 0; i < lines; i++)
                data[i] = 0x0A;

            return data;
        }

        private byte[] AlignLeft() => new byte[] { 0x1B, 0x61, 0x00 };
        private byte[] AlignCenter() => new byte[] { 0x1B, 0x61, 0x01 };
        private byte[] Bold(bool on) => new byte[] { 0x1B, 0x45, on ? (byte)0x01 : (byte)0x00 };
        private byte[] CutPartial() => new byte[] { 0x1D, 0x56, 0x42, 0x00 };

        private byte[] DrawerPulse(int m = 0, int t1 = 120, int t2 = 120)
            => new byte[] { 0x1B, 0x70, (byte)m, (byte)t1, (byte)t2 };

        private byte[] OpenDrawerSequence(int drawerPin = 0)
        {
            List<byte> bytes = new List<byte>();

            if (drawerPin == 1)
            {
                bytes.AddRange(DrawerPulse(1, 120, 120));
            }
            else
            {
                bytes.AddRange(DrawerPulse(0, 120, 120));
            }

            bytes.AddRange(Lf(1));
            return bytes.ToArray();
        }

        private byte[] Text(string text)
        {
            string clean = SanitizeText(text);
            return Encoding.ASCII.GetBytes(clean);
        }

        private string Hr(int cols) => new string('-', cols) + "\n";

        private string LineLR(string left, string right, int cols)
        {
            left ??= "";
            right ??= "";

            if (left.Length + right.Length >= cols)
                return left + " " + right + "\n";

            int spaces = cols - left.Length - right.Length;
            return left + new string(' ', spaces) + right + "\n";
        }

        private string SanitizeText(string input)
        {
            return input
                .Replace("á", "a")
                .Replace("é", "e")
                .Replace("í", "i")
                .Replace("ó", "o")
                .Replace("ú", "u")
                .Replace("Á", "A")
                .Replace("É", "E")
                .Replace("Í", "I")
                .Replace("Ó", "O")
                .Replace("Ú", "U")
                .Replace("ñ", "n")
                .Replace("Ñ", "N")
                .Replace("ü", "u")
                .Replace("Ü", "U")
                .Replace("“", "\"")
                .Replace("”", "\"")
                .Replace("‘", "'")
                .Replace("’", "'")
                .Replace("–", "-")
                .Replace("—", "-");
        }

        private byte[] Qr(string content, int size = 8, int ecc = 49)
        {
            List<byte> bytes = new List<byte>();
            byte[] data = Encoding.ASCII.GetBytes(content);

            int s = Math.Clamp(size, 1, 16);
            int e = Math.Clamp(ecc, 48, 51);

            bytes.AddRange(new byte[] { 0x1D, 0x28, 0x6B, 0x04, 0x00, 0x31, 0x41, 0x32, 0x00 });
            bytes.AddRange(new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x43, (byte)s });
            bytes.AddRange(new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x45, (byte)e });

            int len = data.Length + 3;
            byte pL = (byte)(len & 0xFF);
            byte pH = (byte)((len >> 8) & 0xFF);

            bytes.AddRange(new byte[] { 0x1D, 0x28, 0x6B, pL, pH, 0x31, 0x50, 0x30 });
            bytes.AddRange(data);
            bytes.AddRange(new byte[] { 0x1D, 0x28, 0x6B, 0x03, 0x00, 0x31, 0x51, 0x30 });
            bytes.Add(0x0A);

            return bytes.ToArray();
        }

        private byte[]? TryBuildImageFromBase64(string base64Input, int maxWidth, int paperWidthDots, bool center)
        {
            try
            {
                byte[] imageBytes = ExtractBase64Bytes(base64Input);
                using MemoryStream ms = new MemoryStream(imageBytes);
                using Bitmap original = new Bitmap(ms);

                Bitmap prepared = PrepareBitmap(original, maxWidth, paperWidthDots, center);

                try
                {
                    return RasterImage(prepared);
                }
                finally
                {
                    prepared.Dispose();
                }
            }
            catch
            {
                return null;
            }
        }

        private byte[] ExtractBase64Bytes(string input)
        {
            string clean = input.Trim();

            int commaIndex = clean.IndexOf(',');
            if (clean.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && commaIndex >= 0)
            {
                clean = clean[(commaIndex + 1)..];
            }

            return Convert.FromBase64String(clean);
        }

        private Bitmap PrepareBitmap(Bitmap original, int maxWidth, int paperWidthDots, bool center)
        {
            int targetWidth = original.Width;
            int targetHeight = original.Height;

            if (maxWidth > 0 && targetWidth > maxWidth)
            {
                double ratio = (double)maxWidth / targetWidth;
                targetWidth = maxWidth;
                targetHeight = Math.Max(1, (int)Math.Round(original.Height * ratio));
            }

            using Bitmap resized = new Bitmap(targetWidth, targetHeight);
            using (Graphics g = Graphics.FromImage(resized))
            {
                g.Clear(Color.White);
                g.DrawImage(original, 0, 0, targetWidth, targetHeight);
            }

            // Usar el ancho exacto del papel para centrar correctamente
            int canvasWidth = paperWidthDots;
            Bitmap canvas = new Bitmap(canvasWidth, targetHeight);

            using (Graphics g = Graphics.FromImage(canvas))
            {
                g.Clear(Color.White);

                int x = center
                    ? (canvasWidth - targetWidth) / 2
                    : 0;

                g.DrawImage(resized, x, 0, targetWidth, targetHeight);
            }

            return canvas;
        }
        private byte[] RasterImage(Bitmap bitmap)
        {
            List<byte> output = new List<byte>();

            int width = bitmap.Width;
            int height = bitmap.Height;
            int bytesPerRow = (width + 7) / 8;

            output.Add(0x1D);
            output.Add(0x76);
            output.Add(0x30);
            output.Add(0x00);
            output.Add((byte)(bytesPerRow & 0xFF));
            output.Add((byte)((bytesPerRow >> 8) & 0xFF));
            output.Add((byte)(height & 0xFF));
            output.Add((byte)((height >> 8) & 0xFF));

            for (int y = 0; y < height; y++)
            {
                for (int xByte = 0; xByte < bytesPerRow; xByte++)
                {
                    byte slice = 0;

                    for (int bit = 0; bit < 8; bit++)
                    {
                        int x = xByte * 8 + bit;
                        if (x >= width) continue;

                        Color pixel = bitmap.GetPixel(x, y);
                        int gray = (pixel.R + pixel.G + pixel.B) / 3;

                        if (gray < 180)
                        {
                            slice |= (byte)(0x80 >> bit);
                        }
                    }

                    output.Add(slice);
                }
            }

            output.AddRange(Lf(1));
            return output.ToArray();
        }
    }
}
