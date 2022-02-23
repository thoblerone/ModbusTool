using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusWpf.Common.Helpers 
{
    public enum ClientFunctions
    {
        ReadCoils,
        ReadDiscrete,
        ReadHoldingRegister,
        ReadInputRegister,
        WriteSingleCoil,
        WriteSingleRegister,
        WriteMultipleCoils,
        WriteMultipleRegister,
    };
}
