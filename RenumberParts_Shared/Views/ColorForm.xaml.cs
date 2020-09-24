
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

namespace RenumberParts
{
    /// <summary>
    /// Interaction logic for MainForm.xaml
    /// </summary>
    public partial class ColorForm : Window
    {
        private void Border_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }


        private void Title_Link(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://engworks.com/renumber-parts/");
        }

        private void PrefixBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.PrefixBox.Select(this.PrefixBox.Text.Length, 0);
        }
    }
}
