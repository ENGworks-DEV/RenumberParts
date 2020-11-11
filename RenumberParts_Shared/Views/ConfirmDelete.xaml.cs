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
using System.Windows.Navigation;
using System.Windows.Shapes;
using RenumberParts.Model;


namespace RenumberParts
{
    /// <summary>
    /// Interaction logic for ConfirmDelete.xaml
    /// </summary>
    public partial class ConfirmDelete : Window
    {
        public ConfirmDelete(string message)
        {
            InitializeComponent();
            this.Message.Text = message;
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            //If yes reset all values and close form
            this.DialogResult = true;
            this.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //If no just close the form
            this.DialogResult = false;
            this.Close();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)

        {
            this.DragMove();
        }

    }
}
