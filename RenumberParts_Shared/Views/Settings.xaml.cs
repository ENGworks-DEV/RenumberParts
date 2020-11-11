using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;

namespace RenumberParts
{
    /// <summary>
    /// Interaction logic for MainForm.xaml
    /// </summary>
    public partial class MainFormSettings : Window
    {
        public static System.Drawing.Color ColorSelected;

        public ColorDialog colorDialog = new ColorDialog();

        private ExternalCommandData p_commanddata;

        public Document _doc;

        public UIApplication uiApp;

        public bool open = false;


        public MainFormSettings(ExternalCommandData cmddata_p)
        {
            this.p_commanddata = cmddata_p;
            this.InitializeComponent();
            this.uiApp = cmddata_p.Application;
            UIDocument uiDoc = this.uiApp.ActiveUIDocument;
            this._doc = uiDoc.Document;

        }

        /// <summary>
        /// Button close form when clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
