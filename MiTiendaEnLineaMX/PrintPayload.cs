namespace MiTiendaEnLineaMX
{
    public class PrintPayload
    {
        public string Transport { get; set; } = "usb";
        public string PaperSize { get; set; } = "80";

        public string TextBeforeQr { get; set; } = "";
        public string QrText { get; set; } = "";
        public string TextAfterQr { get; set; } = "";

        public bool Cut { get; set; } = true;
        public bool OpenDrawer { get; set; } = true;
        public int DrawerPin { get; set; } = 0;

        public int QrSize { get; set; } = 7;
        public int QrEcc { get; set; } = 49;

        public string Logo { get; set; } = "";
        public string LogoAlign { get; set; } = "center";
        public int LogoMaxWidth { get; set; } = 160;

        public string Host { get; set; } = "";
        public int Port { get; set; } = 0;
    }
}
