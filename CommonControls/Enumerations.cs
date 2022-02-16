using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Modbus.Common
{
    public enum DisplayFormat
    {
        LED,
        Binary,
        Hex,
        Integer,
        FloatReverse,
        // TODO_Float
        // TODO_Text
    }

    public enum CommunicationMode
    {
        TCP,
        UDP,
        RTU
    }

    public enum Function
    {
        CoilStatus,
        InputStatus,
        HoldingRegister,
        InputRegister
    }
}
