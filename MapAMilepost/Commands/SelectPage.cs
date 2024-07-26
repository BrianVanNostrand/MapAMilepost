using MapAMilepost.Utils;
using MapAMilepost.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;

namespace MapAMilepost.Commands
{
    public class SelectPage : ICommand
    {
        /// <summary>
        /// Viewmodels that will be used to populate the content control in Dockpane1.xaml.
        /// </summary>
        private MainViewModel _viewModel;

        /// <summary>
        /// Set the private main viewmodel variable to reference main viewmodel. 
        /// </summary>
        /// <param name="viewModel"></param>
        public SelectPage(MainViewModel viewModel)
        {
            this._viewModel = viewModel;
        }
        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            TabControl control = ((TabControl)parameter);
            TabItem tabItem = (TabItem)control.SelectedItem;
            string Name = tabItem.Name;
            switch (Name)
            {
                case "MapPointButton":
                    _viewModel.SelectedViewModel = _viewModel.MapPointVM;
                    break;
                case "MapLineButton":
                    _viewModel.SelectedViewModel = _viewModel.MapLineVM;
                    if (_viewModel.MapPointVM.MapToolInfos.SessionActive)
                    {
                        MapToolUtils.InitializeSession(_viewModel.MapPointVM);//end the mapping session
                    }
                    break;
                default:
                    throw new ArgumentException("Control name not found");
            }
        }
    }
}
