
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RenumberParts.Model;
using Xceed.Wpf.Toolkit;

namespace RenumberParts
{
    /// <summary>
    /// Interaction logic for MainForm.xaml
    /// </summary>
    public partial class ColorForm : Window
    {
        public static System.Windows.Media.Color colorSelected;
        public static bool check;
        public ColorForm()
        {
            InitializeComponent();
            chkColor.IsChecked = check;
            ClrPcker.IsOpen = check;
        }

        /// <summary>
        /// Colorize all elements in view grouping them by its prefix
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColorByPrefix_Click(object sender, RoutedEventArgs e)
        {
            ColorByPrfx.ColorInView();
        }
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (this.chkColor.IsChecked.HasValue && this.chkColor.IsChecked.Value)
            {
                if (ClrPcker.SelectedColor.HasValue)
                {
                    colorSelected = ClrPcker.SelectedColor.Value;
                }
            }
            DialogResult = true;
            this.Hide();
        }

        private void chkColor_Checked(object sender, EventArgs e)
        {
            check=chkColor.IsChecked.Value;
            ClrPcker.IsEnabled = check;
            ClrPcker.IsOpen = check;
            if (!check)
            {
                colorSelected=System.Windows.Media.Color.FromArgb(1, 255, 0, 0);
            }
        }
        private void Title_Link(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://engworks.com/renumber-parts/");
        }
    }
}
