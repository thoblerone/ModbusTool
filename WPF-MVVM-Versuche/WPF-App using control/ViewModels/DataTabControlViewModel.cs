using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.PeerToPeer.Collaboration;
using System.Text;
using System.Threading.Tasks;
using Catel;
using Catel.IoC;
using Catel.MVVM;
using Modbus.Common;
using WPF_App_using_control.Helpers;
using WPF_App_using_control.Models;

namespace WPF_App_using_control.ViewModels
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
            ClearDataCommand = new TaskCommand(() => ClearDataExecuteAsync());
            DisplayFormatItemSource = EnumHelpers.EnumTypeDescriptionToItemSourceArray(typeof(DisplayFormat));

            if (registerDataService is null)
            {
                registerDataService = new RegisterDataService();
                ServiceLocator.Default.RegisterInstance(typeof(IRegisterDataService), registerDataService);
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

        public DisplayFormat DisplayFormat { get; set; } = DisplayFormat.Integer;

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
            for (var i = StartAddress; i + StartAddress < RegisterDataService.RegisterData.Length && i < DataLength; i++)
            {
                RegisterDataService.RegisterData[i].RegisterValue = 0;
            }

        }

        private Task ApplyAddressSelectionExecuteAsync()
        {
            RegisterModels.Clear();
            for (var i = StartAddress;
                i < RegisterDataService.RegisterData.Length &&
                i < DataLength + StartAddress;
                i++)
            {
                var nCoils = DisplayFormat == DisplayFormat.LED ? 16 : 1;

                var model = new RegisterDisplayModel(RegisterDataService, i)
                {
                    RepresentationKind = DisplayFormat,
                    CoilNumber = 0
                };
                RegisterModels.Add(model);

                // Add further coils, if display format is LED
                for (var coil = 1; coil < nCoils; coil++)
                {
                    model = new RegisterDisplayModel(RegisterDataService, i)
                    {
                        RepresentationKind = DisplayFormat,
                        CoilNumber = coil
                    };
                    RegisterModels.Add(model);
                }
                // the floating point representation consumes
                // two registers for each value, so skip every second
                if (DisplayFormat == DisplayFormat.FloatReverse)
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
