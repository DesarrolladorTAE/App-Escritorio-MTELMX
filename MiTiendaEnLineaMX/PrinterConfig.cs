namespace MiTiendaEnLineaMX
{
    public enum PrinterMode
    {
        Tcp,
        Usb
    }

    public enum PaperSize
    {
        Mm58,
        Mm80
    }

    public class PrinterConfig
    {
        public PrinterMode Mode { get; set; } = PrinterMode.Tcp;
        public PaperSize PaperSize { get; set; } = PaperSize.Mm80;

        public string TcpHost { get; set; } = "";
        public int TcpPort { get; set; } = 9100;

        public string PrinterName { get; set; } = "";

        public bool Cut { get; set; } = true;
        public bool OpenDrawer { get; set; } = false;

        public string QrText { get; set; } = "https://mitiendaenlineamx.com.mx";
        public string LogoBase64 { get; set; } = "";
    }
}
