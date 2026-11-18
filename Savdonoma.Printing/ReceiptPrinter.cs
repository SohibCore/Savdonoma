using ESCPOS_NET.Emitters;
using ESCPOS_NET.Utilities;
using System.Globalization;
using Savdonoma.Core.Printer;

namespace Savdonoma.Printing
{
    public class ReceiptPrinter : IReceiptPrinter
    {
        private readonly string _print;

        #region printer
        private const int Width = 42;       // Font A: printerga qarab 42 yoki 48
        private const int SmallWidth = 56;  // Font B: printerga qarab sinab to'g'rilang

        private static readonly NumberFormatInfo Nfi =
            new() { NumberGroupSeparator = " ", NumberDecimalSeparator = "," };

        private static string Line() => new string('-', Width);
        private static string Money(decimal v) => v.ToString("N2", Nfi);

        private static string LeftRight(string l, string r, int width = Width) =>
            l + new string(' ', Math.Max(1, width - l.Length - r.Length)) + r;

        private static string Right(string s) => s.PadLeft(Width);

        // Uzun matnni width bo'yicha qatorlarga bo'ladi
        private static IEnumerable<string> Wrap(string text, int width)
        {
            if (string.IsNullOrEmpty(text)) yield break;
            for (int pos = 0; pos < text.Length; pos += width)
                yield return text.Substring(pos, Math.Min(width, text.Length - pos));
        }
        #endregion

        public ReceiptPrinter(string print)
        {
            _print = print;
        }

        public bool PrintCheck(ReceiptModel model, CancellationToken cancellation)
        {
            var e = new EPSON();
            var parts = new List<byte[]>();

            parts.Add(e.Initialize());

            // ===== Sarlavha =====
            parts.Add(e.CenterAlign());
            parts.Add(e.SetStyles(PrintStyle.Bold | PrintStyle.DoubleHeight));
            parts.Add(e.PrintLine(model.ShopName));
            parts.Add(e.SetStyles(PrintStyle.None));
            foreach (var line in Wrap(model.Address, Width))
                parts.Add(e.PrintLine(line));

            parts.Add(e.PrintLine(Line()));

            // ===== Chek ma'lumotlari =====
            parts.Add(e.LeftAlign());
            parts.Add(e.PrintLine("Kassir: " + model.Cashier));
            parts.Add(e.PrintLine("Sana: " + model.Date.ToString("dd.MM.yyyy HH:mm:ss")));
            parts.Add(e.PrintLine("INN: " + model.INN));
            parts.Add(e.PrintLine("Chek raqami: " + model.ReceiptNumber));
            parts.Add(e.PrintLine(Line()));

            // ===== Mahsulotlar =====
            decimal totalVat = 0;

            for (int i = 0; i < model.Items.Count; i++)
            {
                var item = model.Items[i];
                var sum = item.Price * item.Quantity;
                var vat = Math.Round(sum * model.VatRate / (100 + model.VatRate), 2);
                totalVat += vat;

                // 1) Nom (uzun bo'lsa keyingi qatorga o'tadi)
                foreach (var line in Wrap($"{i + 1}. {item.Name}", Width))
                    parts.Add(e.PrintLine(line));

                // 2) Miqdor x narx = summa (o'ngda)
                parts.Add(e.PrintLine(Right($"{item.Quantity}X{Money(item.Price)}={Money(sum)}")));

                // 3) QQS (o'ngda)
                parts.Add(e.PrintLine(Right($"shu jumladan QQS {model.VatRate} % {Money(vat)}")));

                // 4) Mayda shriftdagi qatorlar (bo'sh bo'lsa chiqmaydi)
                parts.Add(e.SetStyles(PrintStyle.FontB));
                /*if (!string.IsNullOrWhiteSpace(item.Unit))
                    foreach (var line in Wrap("O'lchov birligi: " + item.Unit, SmallWidth))
                        parts.Add(e.PrintLine(line));*/
                if (!string.IsNullOrWhiteSpace(item.Mxik))
                    foreach (var line in Wrap("MXIK: " + item.Mxik, SmallWidth))
                        parts.Add(e.PrintLine(line));
                if (!string.IsNullOrWhiteSpace(item.Barcode))
                    foreach (var line in Wrap("Shtrix kod: " + item.Barcode, SmallWidth))
                        parts.Add(e.PrintLine(line));
                parts.Add(e.SetStyles(PrintStyle.None));
            }

            parts.Add(e.PrintLine(Line()));

            // ===== JAMI (katta) =====
            // DoubleWidth da har belgi 2 baravar keng, shuning uchun Width / 2
            parts.Add(e.SetStyles(PrintStyle.Bold | PrintStyle.DoubleHeight | PrintStyle.DoubleWidth));
            parts.Add(e.PrintLine(LeftRight("JAMI:", " = " + Money(model.Total), Width / 2)));
            parts.Add(e.SetStyles(PrintStyle.None));

            // ===== Jami QQS va to'lov turlari =====
            parts.Add(e.PrintLine(LeftRight("Jami QQS", Money(totalVat))));
            parts.Add(e.PrintLine(Line()));
            parts.Add(e.PrintLine(LeftRight("NAQD:", Money(model.Cash))));
            parts.Add(e.PrintLine(LeftRight("BANK KARTA:", Money(model.Card))));
            parts.Add(e.PrintLine(Line()));
            parts.Add(e.PrintLine(LeftRight("JAMI:", Money(model.Total))));

            parts.Add(e.FeedLines(3));
            parts.Add(e.PartialCutAfterFeed(1));

            var bytes = ByteSplicer.Combine(parts.ToArray());
            return RawPrinterHelper.SendBytes(_print, bytes);
        }
    }
}