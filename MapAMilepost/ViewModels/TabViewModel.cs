using ArcGIS.Desktop.Framework.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapAMilepost.ViewModels
{
    public class TabViewModel:Utils.ViewModelBase
    {
        private string _header;
        private Utils.ViewModelBase _content;
        public string Header
        {
            get => _header;
            set
            {
                _header = value;
                OnPropertyChanged(nameof(Header));
            }
        }

        public Utils.ViewModelBase Content
        {
            get => _content;
            set
            {
                _content = value;
                OnPropertyChanged(nameof(Content));
            }
        }

        public TabViewModel(string header, Utils.ViewModelBase viewModel)
        {
            Header = header;
            Content = viewModel;
        }
    }
}
