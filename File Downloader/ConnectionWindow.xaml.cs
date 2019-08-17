using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MahApps.Metro;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Behaviours;


namespace File_Downloader
{
    /// <summary>
    /// Логика взаимодействия для ConnectionWindow.xaml
    /// </summary>
    public partial class ConnectionWindow : CustomDialog
    {

        public ConnectionWindow()
        {
            InitializeComponent();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.resultConnectionWindow = 3;
            (this.OwningWindow ?? (MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(this);
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.resultConnectionWindow = 1;
            (this.OwningWindow ?? (MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(this);
        }

        private void WaitButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.resultConnectionWindow = 2;
            (this.OwningWindow ?? (MainWindow)Application.Current.MainWindow).HideMetroDialogAsync(this);
        }
    }
}
