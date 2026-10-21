namespace Savdonoma.Core.Printer
{
    public interface IReceiptPrinter
    {
        public bool PrintCheck(ReceiptModel model, CancellationToken cancellation);
    }
}
