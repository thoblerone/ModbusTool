using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Catel.Services;
using ModbusWpf.Common.Helpers;
using ModbusWpf.Common.ViewModels;

namespace ModbusWpf.Client.ViewModels
{
    class ClientViewModel : BaseViewModel
    {
        #region Constructors
        public ClientViewModel() : this(null, null)
        {
        }

        public ClientViewModel(IDispatcherService dispatcherService, IRegisterDataService registerDataService)
            : base(dispatcherService, registerDataService)
        {
        }
        #endregion // Constructors

        #region Catel overrides
        protected override async Task InitializeAsync()
        {
            await base.InitializeAsync().ConfigureAwait(false);

            Title = $"Modbus Client ({Assembly.GetExecutingAssembly().GetName().Version})";
        
            MasterOptionsVisibility = Visibility.Visible;
            SlaveOptionsVisibility = Visibility.Collapsed;
        }
        #endregion // Catel overrides

        #region BaseViewModel overrides
        public override string IconPath => "M 251.92969 24 C 224.97289 23.9208 199.02276 34.237381 179.47656 52.800781 L 175.8125 52.800781 C 156.8729 52.800781 141.51848 68.152197 141.51758 87.091797 C 141.51758 96.187197 145.1302 104.91235 151.5625 111.34375 L 168 127.78125 L 168 241.69922 L 163.88867 245.94141 C 110.98307 300.19612 81.442109 373.02167 81.599609 448.80078 C 81.599609 452.77698 84.824581 456 88.800781 456 L 391.19922 456 C 395.17542 456 398.40039 452.77698 398.40039 448.80078 C 398.55699 373.02258 369.01934 300.19776 316.11914 245.93945 L 312 241.69922 L 312 142.18164 L 325.63672 128.55273 C 345.82012 108.38193 345.83186 75.667775 325.66016 55.484375 C 323.97536 53.797775 322.17514 52.231169 320.27344 50.792969 L 326.0332 33.511719 C 327.3094 29.746119 325.29189 25.657959 321.52539 24.380859 C 320.77749 24.127059 319.99032 23.9991 319.19922 24 L 251.92969 24 z M 290.96094 91.017578 C 295.29174 99.211178 297.56991 108.33231 297.59961 117.59961 L 297.59961 234.81641 L 240 254.01172 L 182.40039 234.81641 L 182.40039 127.32812 L 207.14648 96.367188 L 248.09961 100.91797 C 262.57431 102.49657 277.17059 99.319187 289.67969 91.867188 C 290.11889 91.600786 290.52894 91.291178 290.96094 91.017578 z M 303.83203 251.19922 C 302.55003 287.29822 293.00092 322.62027 275.91992 354.44727 L 265.32031 372.27148 C 257.81531 383.71548 249.344 394.49709 240 404.49609 C 230.636 394.47609 222.14981 383.67022 214.63281 372.19922 C 214.17965 371.47765 204.08008 354.44922 204.08008 354.44922 C 187.00008 322.62122 177.45097 287.29917 176.16797 251.20117 L 240 271.29688 L 303.83203 251.19922 z M 240 271.29688 L 225.5918 370.52734 C 225.3438 371.12334 225.02181 371.68422 224.63281 372.19922 L 240 404.49609 L 255.32031 372.27148 C 254.94231 371.73148 254.6362 371.14534 254.4082 370.52734 L 240 271.29688 z ";
        public override Color IconColor => Color.FromRgb(0, 0xc7, 0xff);
        #endregion // BaseViewModel overrides


    }
}
