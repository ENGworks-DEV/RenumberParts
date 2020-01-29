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

namespace RenumberParts
{
    /// <summary>
    /// Interaction logic for SuffixContent.xaml
    /// </summary>
    public partial class SuffixContent : Window
    {
        public SuffixContent()
        {
            InitializeComponent();
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            //If no just close the form
            this.Close();
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)

        {
            this.DragMove();
        }

    }
}
