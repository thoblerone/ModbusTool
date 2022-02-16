using System;
using System.CodeDom;
using System.Diagnostics;
using System.Windows.Controls;
using Catel.Data;
using Catel.MVVM.Converters;

namespace WPF_App_using_control.Helpers
{
    public interface IRegisterDataService
    {
        RegisterDataValue[] RegisterData { get; set; }
    }


    public class RegisterDataValue : ModelBase
    {
        public RegisterDataValue(ushort value)
        {
            RegisterValue = value;
        }

        public delegate void RegisterValueChangedHandler(ushort oldValue, ushort newValue);
        public event RegisterValueChangedHandler RegisterValueChanged;

        private ushort _registerValue;

        public ushort RegisterValue
        {
            get => _registerValue;
            set
            {
                if (value != _registerValue)
                {
                    Trace.WriteLine($"Changing Register Value from {_registerValue} to {value}");
                    var oldValue = _registerValue;

                    _registerValue = value;

                    var handler = RegisterValueChanged;
                    handler?.Invoke(oldValue, value);
                }
            }
        }
    }

    public class RegisterDataService : IRegisterDataService
    {
        public static int NumRegistersHeld = 50;

        public RegisterDataService()
        {
            RegisterData = new RegisterDataValue[NumRegistersHeld];

            for (ushort i = 0; i < NumRegistersHeld; i++)
            {
                RegisterData[i] = new RegisterDataValue(i);
            }
        }

        public RegisterDataValue[] RegisterData { get; set; }
    }



    public class BuyerList 
    {
        public delegate void BuyerSelectedEventHandler(object sender, EventArgs e);

        public event BuyerSelectedEventHandler BuyerSelected;


        private void OnBuyerSelected(EventArgs e)
        {
            BuyerSelected?.Invoke(this, EventArgs.Empty);
        }

        protected void lbBuyerList_SelectedIndexChanged(object sender, EventArgs e)
        {
            OnBuyerSelected(e);
        }
    }
}