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
        private const int Width = 42;   // printerga qarab 42 yoki 48
        private const int QtyW = 6, PriceW = 14, NameW = Width - QtyW - PriceW;
        private static string Line() => new string('-', Width);
        private static string Money(decimal v) =>
            string.Format(CultureInfo.InvariantCulture, "{0:N0}", v)
                  .Replace(',', ' ');

        private static string LeftRight(string l, string r) =>
            l + new string(' ', Math.Max(1, Width - l.Length - r.Length)) + r;
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
            parts.Add(e.CenterAlign());
            parts.Add(e.SetStyles(PrintStyle.Bold | PrintStyle.DoubleHeight));
            parts.Add(e.PrintLine(model.ShopName));
            parts.Add(e.SetStyles(PrintStyle.None));

            parts.Add(e.CenterAlign());
            parts.Add(e.PrintLine(Line()));
            parts.Add(e.PrintLine("Sana: " + DateTime.Now.ToString("dd.MM.yyyy")));
            parts.Add(e.PrintLine("Vaqt: " + DateTime.Now.ToString("HH:mm")));
            parts.Add(e.PrintLine("Sotuvchi: " + model.Cashier));

            foreach (var item in model.Items)
            {
                var first = item.Name.Length > NameW ? item.Name[..NameW] : item.Name;
                parts.Add(e.PrintLine(first.PadRight(NameW)
                    + item.Quantity.ToString().PadLeft(QtyW)
                    + Money(item.Price).PadLeft(PriceW)));

                if (item.Name.Length > NameW)
                    parts.Add(e.PrintLine(item.Name[NameW..]));
            }

            parts.Add(e.PrintLine(Line()));
            parts.Add(e.PrintLine(LeftRight("To'lov turi:", model.PaymentType.ToString())));
            parts.Add(e.PrintLine(LeftRight("Jami:", Money(model.Total) + " UZS")));
            parts.Add(e.PrintLine(LeftRight("To'landi:", Money(model.Paid) + " UZS")));
            if (model.Change > 0)
                parts.Add(e.PrintLine(LeftRight("Qaytim:", Money(model.Change) + " UZS")));

            parts.Add(e.PrintLine(Line()));
            parts.Add(e.FeedLines(3));
            parts.Add(e.PartialCutAfterFeed(1));

            var bytes = ByteSplicer.Combine(parts.ToArray());
            return RawPrinterHelper.SendBytes(_print, bytes);
        }
    }
}
