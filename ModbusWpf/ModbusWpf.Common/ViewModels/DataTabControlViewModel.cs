using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Catel.IoC;
using Catel.MVVM;
using Modbus.Ioc.Interfaces;
using Modbus.Ioc.Services;
using ModbusWpf.Common.Helpers;
using ModbusWpf.Common.Models;

namespace ModbusWpf.Common.ViewModels
{
    public class DataTabControlViewModel : ViewModelBase
    {
        #region Constructors

        public DataTabControlViewModel() : this(null)
        {
        }
        public DataTabControlViewModel(IModbusRegisterDataService modbusRegisterDataService)
        {
            ApplyAddressSelectionCommand = new TaskCommand(() => ApplyAddressSelectionExecuteAsync());

            DisplayFormat = new DisplayFormat();
            DisplayFormat = Helpers.DisplayFormat.UInt16;

            ClearDataCommand = new TaskCommand(() => ClearDataExecuteAsync());
            DisplayFormatItemSource = Enum.GetValues(typeof(DisplayFormat));


            if (modbusRegisterDataService is null)
            {
                modbusRegisterDataService = ServiceLocator.Default.TryResolveType<IModbusRegisterDataService>();
                if (modbusRegisterDataService is null)
                {
                    modbusRegisterDataService = new ModbusRegisterDataService();
                    ServiceLocator.Default.RegisterInstance(typeof(IModbusRegisterDataService), modbusRegisterDataService);
                }
            }
            
            ModbusRegisterDataService = modbusRegisterDataService;
            RegisterModels = new ObservableCollection<RegisterDisplayModel>();
        }

        public IModbusRegisterDataService ModbusRegisterDataService { get; }

        #endregion

        #region Properties

        public ushort DataLength
        {
            get => _dataLength;
            set
            {
                if (_dataLength == value)
                    return;

                // the UI should bind using UpdateSourceTrigger=LostFocus
                // to avoid too frequent UI rebuilds
                _dataLength = value;

                ApplyAddressSelectionCommand.Execute();
                RaisePropertyChanged(nameof(DataLength));
            }
        }

        private int _startAddress;
        private ushort _dataLength = 32;

        public int StartAddress
        {
            get => _startAddress;
            set
            {
                if (_startAddress == value)
                    return;

                // the UI should bind using UpdateSourceTrigger=LostFocus
                // to avoid too frequent UI rebuilds
                _startAddress = value;
                
                ApplyAddressSelectionCommand.Execute();
                RaisePropertyChanged(nameof(StartAddress));
            }
        }

        public DisplayFormat? DisplayFormat { get; set; }

        public object DisplayFormatItemSource { get; }

        public ObservableCollection<RegisterDisplayModel> RegisterModels { get; }

        #endregion

        #region Commands

        public TaskCommand ApplyAddressSelectionCommand { get; }

        public TaskCommand ClearDataCommand { get; }

        #endregion

        #region Events

        public event EventHandler OnApplyAddressAndRange;

        #endregion

        private async Task ClearDataExecuteAsync()
        {
            for (var i = StartAddress; i + StartAddress < ModbusRegisterDataService.RegisterData.Length && i < DataLength+ StartAddress; i++)
            {
                ModbusRegisterDataService[i] = 0;
            }

            await Task.CompletedTask;
        }

        private Task ApplyAddressSelectionExecuteAsync()
        {
            if (!DisplayFormat.HasValue) // true for the dummy tab
                return Task.CompletedTask;

            RegisterModels.Clear();
            for (var i = StartAddress;
                i < ModbusRegisterDataService.RegisterData.Length &&
                i < DataLength + StartAddress;
                i++)
            {
                var nCoils = (ModbusWpf.Common.Helpers.DisplayFormat.LED == DisplayFormat) ? 16 : 1;

                var model = new RegisterDisplayModel(ModbusRegisterDataService, i)
                {
                    RepresentationKind = DisplayFormat.Value,
                    CoilNumber = 0
                };
                RegisterModels.Add(model);

                // Add further coils, if display format is LED
                for (var coil = 1; coil < nCoils; coil++)
                {
                    model = new RegisterDisplayModel(ModbusRegisterDataService, i)
                    {
                        RepresentationKind = DisplayFormat.Value,
                        CoilNumber = coil
                    };
                    RegisterModels.Add(model);
                }
                // the floating point representations consume
                // two registers for each value, so skip every second
                if (DisplayFormat.Value is ModbusWpf.Common.Helpers.DisplayFormat.FloatReverse 
                                        // TODO: Questionable or ModbusWpf.Common.Helpers.DisplayFormat.Float
                                        or ModbusWpf.Common.Helpers.DisplayFormat.Int32)
                    i++;
            }

            OnApplyAddressAndRange?.Invoke(this, EventArgs.Empty);

            return Task.CompletedTask;
        }

        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(false);
            await ApplyAddressSelectionExecuteAsync().ConfigureAwait(false);
        }
    }
}
