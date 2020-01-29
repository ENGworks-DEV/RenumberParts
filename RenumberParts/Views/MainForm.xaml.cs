


using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;
using System.Drawing;
using System.Windows.Media;
using System.Windows.Input;

namespace RenumberParts
{
    /// <summary>
    /// Interaction logic for MainForm.xaml
    /// </summary>
    public partial class MainForm : Window
    {
        public static System.Drawing.Color ColorSelected;
        public System.Windows.Forms.ColorDialog colorDialog = new ColorDialog();
        

        public MainForm()
        {

            this.DataContext = this;
            InitializeComponent();
            var conf = tools.readConfig();

            //Set as default red color
            ColorSelected = System.Drawing.Color.FromArgb(1,255,0,0);
            

            if (conf != null && conf.Count == 3)
            {
                this.PrefixBox.Text = conf[0];
                this.SeparatorBox.Text = conf[1];
                this.NumberBox.Text = conf[2];
            }
        }

        public string projectVersion = CommonAssemblyInfo.Number;
        public string ProjectVersion
        {
            get { return projectVersion; }
            set { projectVersion = value; }
        }
        /// <summary>
        /// Check if the suffix is a number or not
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void textChangedEventHandler(object sender, TextChangedEventArgs args)
        {

            int c;
            bool isNumeric = int.TryParse(this.NumberBox.Text, out c);

            if (isNumeric)
            {
                tools.count = c;
            }
            else
            {
                SuffixContent suffixContentForm = new SuffixContent();
                suffixContentForm.Topmost = true;
                suffixContentForm.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                suffixContentForm.ShowDialog();
                //MessageBox.Show("Number field should contain only numbers");
            }
        }


        /// <summary>
        /// Reset values in view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {    
            tools.resetView();
        }



        internal static string Separator { get; set; }

       


            /// <summary>
            /// Add spool number to element
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void AddButton_Click(object sender, RoutedEventArgs e)
        {

            System.Drawing.Color color = ColorSelected;

            this.Hide();
            int c;
            bool isNumeric = int.TryParse(this.NumberBox.Text, out c);

            if (isNumeric)
            {
                if (tools.count < int.Parse(this.NumberBox.Text))
                {
                    tools.count = c;
                }

                try
                {
                    using (Autodesk.Revit.DB.Transaction AssingPartNumberT = new Autodesk.Revit.DB.Transaction(tools.uidoc.Document, "Assing Part Number"))
                    {
                        AssingPartNumberT.Start();
                        tools.AddToSelection();

                        var partNumber = tools.createNumbering(this.PrefixBox.Text,this.SeparatorBox.Text, tools.count, this.NumberBox.Text.Length);


                        //tools.AssingPartNumber(tools.selectedElement, partNumber);

                        foreach (Autodesk.Revit.DB.Element x in tools.selectedElements)
                        {
                            tools.AssingPartNumber(x, partNumber);
                        }


                        tools.count += 1;
                        //count 5, pad left
                        
                        int leadingZeros = NumberBox.Text.Length >1 ? this.NumberBox.Text.Length - tools.count.ToString().Length : 0;
                        this.NumberBox.Text = (new string('0', leadingZeros)) + tools.count.ToString();

                        tools.writeConfig(this.PrefixBox.Text,this.SeparatorBox.Text, this.NumberBox.Text);

                        AssingPartNumberT.Commit();

                            
                        
                    }
                }
                catch (System.Exception ex)
                {
                    //MessageBox.Show(ex.Message);
                }

            }
            else
            {
                SuffixContent suffixContentForm = new SuffixContent();
                suffixContentForm.Topmost = true;
                suffixContentForm.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                suffixContentForm.ShowDialog();
                //MessageBox.Show("Number field should contain only numbers");
            }
            
            this.ShowDialog();
        }


        /// <summary>
        /// Displace all values up
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DiplaceUp_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            try
            {
                using (Autodesk.Revit.DB.Transaction DisplaceUp = new Autodesk.Revit.DB.Transaction(tools.uidoc.Document, "Displace Up Part Number"))
            {
                DisplaceUp.Start();
                tools.SetElementsUpStream();
                DisplaceUp.Commit();
            }
            }
            catch 
            {

            }

            this.ShowDialog();
        }


    
        /// <summary>
        /// Desplace all values down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DiplaceDn_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            try
            { 
            using (Autodesk.Revit.DB.Transaction DisplaceUp = new Autodesk.Revit.DB.Transaction(tools.uidoc.Document, "Displace Up Part Number"))
            {
                DisplaceUp.Start();
                tools.SetElementsDnStream();
                DisplaceUp.Commit();
            }


            }
            catch 
            {

            }
            this.ShowDialog();
        }


        /// <summary>
        /// Create an instance of the confirmDeleteForm.xaml
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            ConfirmDelete confirmDeleteForm = new ConfirmDelete();
            confirmDeleteForm.Topmost = true;
            confirmDeleteForm.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            confirmDeleteForm.ShowDialog();
        }


        /// <summary>
        /// Show Color dialog and set selected color to variable
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
             
            colorDialog.ShowDialog();
            ColorSelected = colorDialog.Color;
        }

     
        /// <summary>
        /// Writes string used as separator to an internal field
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //        Separator = this.SeparatorBox.Text;    
        //}


        /// <summary>
        /// Colorize all elements in view grouping them by its prefix
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColorByPrefix_Click(object sender, RoutedEventArgs e)
        {
            tools.ColorInView();
        }

        public void SeparatorBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Separator = this.SeparatorBox.Text;
        }

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
    }
}
