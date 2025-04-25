using CommunityToolkit.Mvvm.ComponentModel;
using Mutagen.Bethesda.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DioramaEngine.Models
{
    public partial class Master : ObservableObject
    {
        [ObservableProperty]
        private ModKey key;

        [ObservableProperty]
        private bool isChecked;

        [ObservableProperty]
        private bool isEnabled;

        public Master(bool forceEnabled)
        {
            IsChecked = forceEnabled;
            isEnabled = !forceEnabled;
        }
    }
}
