using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MiTiendaEnLineaMX
{
    public class PrintPayload
    {
        [JsonPropertyName("transport")]
        public string Transport { get; set; } = "usb";

        [JsonPropertyName("paper")]
        public PrintPaper Paper { get; set; } = new();

        [JsonPropertyName("printer")]
        public PrintPrinter Printer { get; set; } = new();

        [JsonPropertyName("textBeforeQr")]
        public string TextBeforeQr { get; set; } = "";

        [JsonPropertyName("textAfterQr")]
        public string TextAfterQr { get; set; } = "";

        [JsonPropertyName("qrs")]
        public List<PrintQr> Qrs { get; set; } = new();

        [JsonPropertyName("qrText")]
        public string QrText { get; set; } = "";

        [JsonPropertyName("qrSize")]
        public int QrSize { get; set; } = 7;

        [JsonPropertyName("qrEcc")]
        public int QrEcc { get; set; } = 49;

        [JsonPropertyName("cut")]
        public bool Cut { get; set; } = true;

        [JsonPropertyName("openDrawer")]
        public bool OpenDrawer { get; set; } = true;

        [JsonPropertyName("drawerPin")]
        public int DrawerPin { get; set; } = 0;

        [JsonPropertyName("logo")]
        public string Logo { get; set; } = "";

        [JsonPropertyName("logoAlign")]
        public string LogoAlign { get; set; } = "center";

        [JsonPropertyName("logoMaxWidth")]
        public int LogoMaxWidth { get; set; } = 160;

        [JsonPropertyName("meta")]
        public PrintMeta? Meta { get; set; }

        [JsonIgnore]
        public string PaperSize
        {
            get => Paper != null ? Paper.Size.ToString() : "80";
            set
            {
                if (Paper == null) Paper = new PrintPaper();

                if (int.TryParse(value, out int parsed))
                    Paper.Size = parsed;
                else
                    Paper.Size = 80;
            }
        }

        [JsonIgnore]
        public string Host
        {
            get => Printer?.Ip ?? "";
            set
            {
                if (Printer == null) Printer = new PrintPrinter();
                Printer.Ip = value;
            }
        }

        [JsonIgnore]
        public int Port
        {
            get => Printer?.Port ?? 0;
            set
            {
                if (Printer == null) Printer = new PrintPrinter();
                Printer.Port = value;
            }
        }
    }

    public class PrintPaper
    {
        [JsonPropertyName("size")]
        public int Size { get; set; } = 80;

        [JsonPropertyName("charsPerLine")]
        public int CharsPerLine { get; set; } = 48;
    }

    public class PrintPrinter
    {
        [JsonPropertyName("ip")]
        public string? Ip { get; set; }

        [JsonPropertyName("port")]
        public int? Port { get; set; }
    }

    public class PrintQr
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("text")]
        public string Text { get; set; } = "";

        [JsonPropertyName("size")]
        public int Size { get; set; } = 7;

        [JsonPropertyName("caption")]
        public string Caption { get; set; } = "";

        [JsonPropertyName("align")]
        public string Align { get; set; } = "center";

        [JsonPropertyName("ecc")]
        public int Ecc { get; set; } = 49;
    }

    public class PrintMeta
    {
        [JsonPropertyName("sale_id")]
        public int? SaleId { get; set; }

        [JsonPropertyName("store_id")]
        public int? StoreId { get; set; }

        [JsonPropertyName("branch_id")]
        public int? BranchId { get; set; }

        [JsonPropertyName("ticket_id")]
        public int? TicketId { get; set; }

        [JsonPropertyName("paper_size")]
        public int? PaperSize { get; set; }

        [JsonPropertyName("chars_per_line")]
        public int? CharsPerLine { get; set; }

        [JsonPropertyName("qr_factura_enabled")]
        public bool? QrFacturaEnabled { get; set; }

        [JsonPropertyName("qr_sitio_enabled")]
        public bool? QrSitioEnabled { get; set; }

        [JsonPropertyName("isCancelled")]
        public bool? IsCancelled { get; set; }

        [JsonPropertyName("isReturned")]
        public bool? IsReturned { get; set; }

        [JsonPropertyName("isEn")]
        public bool? IsEn { get; set; }
    }
}
