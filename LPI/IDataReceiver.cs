using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LPI
{
    public interface IDataReceiver
    {
        void ReceiveBarcode(string barcode);
    }
}
