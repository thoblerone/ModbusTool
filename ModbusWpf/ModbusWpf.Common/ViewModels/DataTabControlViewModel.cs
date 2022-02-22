using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Catel.IoC;
using Catel.MVVM;
using Modbus.Common;
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
        public DataTabControlViewModel(IRegisterDataService registerDataService)
        {
            ApplyAddressSelectionCommand = new TaskCommand(() => ApplyAddressSelectionExecuteAsync());

            DisplayFormat = new DisplayFormat();
            DisplayFormat = Modbus.Common.DisplayFormat.Integer;

            ClearDataCommand = new TaskCommand(() => ClearDataExecuteAsync());
            DisplayFormatItemSource = Enum.GetValues(typeof(DisplayFormat));


            if (registerDataService is null)
            {
                registerDataService = ServiceLocator.Default.TryResolveType<IRegisterDataService>();
                if (registerDataService is null)
                {
                    registerDataService = new RegisterDataService();
                    ServiceLocator.Default.RegisterInstance(typeof(IRegisterDataService), registerDataService);
                }
            }
            
            RegisterDataService = registerDataService;
            RegisterModels = new ObservableCollection<RegisterDisplayModel>();

            //if (CatelEnvironment.IsInDesignMode)
            {
                for (ushort i = 0; i < 25; i++)
                {
                    registerDataService.RegisterData[i].RegisterValue = i;
                }
            }
        }

        public IRegisterDataService RegisterDataService { get; }

        #endregion

        #region Properties
        public ushort DataLength { get; set; } = 32;
        public int StartAddress { get; set; } = 0;

        public bool ShowDataLength { get; set; } = true;

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
            for (var i = StartAddress; i + StartAddress < RegisterDataService.RegisterData.Length && i < DataLength+ StartAddress; i++)
            {
                RegisterDataService.RegisterData[i].RegisterValue = 0;
            }

            await Task.CompletedTask;
        }

        private Task ApplyAddressSelectionExecuteAsync()
        {
            if (!DisplayFormat.HasValue) // true for the dummy tab
                return Task.CompletedTask;

            RegisterModels.Clear();
            for (var i = StartAddress;
                i < RegisterDataService.RegisterData.Length &&
                i < DataLength + StartAddress;
                i++)
            {
                var nCoils = (Modbus.Common.DisplayFormat.LED == DisplayFormat) ? 16 : 1;

                var model = new RegisterDisplayModel(RegisterDataService, i)
                {
                    RepresentationKind = DisplayFormat.Value,
                    CoilNumber = 0
                };
                RegisterModels.Add(model);

                // Add further coils, if display format is LED
                for (var coil = 1; coil < nCoils; coil++)
                {
                    model = new RegisterDisplayModel(RegisterDataService, i)
                    {
                        RepresentationKind = DisplayFormat.Value,
                        CoilNumber = coil
                    };
                    RegisterModels.Add(model);
                }
                // the floating point representation consumes
                // two registers for each value, so skip every second
                if (DisplayFormat.Value == Modbus.Common.DisplayFormat.FloatReverse)
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
