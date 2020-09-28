
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
        public System.Drawing.Color colorSelected;
        public ColorForm()
        {
             
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
            DialogResult = true;
            this.Close();
        }


        private void Title_Link(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://engworks.com/renumber-parts/");
        }
    }
}
