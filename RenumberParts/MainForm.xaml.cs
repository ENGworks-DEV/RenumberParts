//if not exist "$(AppData)\Autodesk\REVIT\Addins\$(ConfigurationName)\ENGworks" mkdir  "$(AppData)\Autodesk\REVIT\Addins\$(ConfigurationName)\ENGworks\" 
//if exist "$(AppData)\Autodesk\REVIT\Addins\$(ConfigurationName)" copy "$(ProjectDir)$(OutputPath)*.dll" "$(AppData)\Autodesk\REVIT\Addins\$(ConfigurationName)\ENGworks"
//copy "$(ProjectDir)icons8-hashtag-52.png" "$(AppData)\Autodesk\REVIT\Addins\$(ConfigurationName)\ENGworks"


using System.Windows;
using System.Windows.Controls;

namespace RenumberParts
{
    /// <summary>
    /// Interaction logic for MainForm.xaml
    /// </summary>
    public partial class MainForm : Window
    {
        public MainForm()
        {
            InitializeComponent();
            var conf = tools.readConfig();
            if (conf != null && conf.Count == 2)
            {
                this.PrefixBox.Text = conf[0];
                this.NumberBox.Text = conf[1];
            }

        }

        private void textChangedEventHandler(object sender, TextChangedEventArgs args)
        {

            int c;
            bool isNumeric = int.TryParse(this.NumberBox.Text, out c);

            if (isNumeric)
            {
                tools.count = c;
            }
            else { MessageBox.Show("Number field should contain a number"); }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            
            tools.resetView();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
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

                        var partNumber = tools.createNumbering(this.PrefixBox.Text, tools.count, this.NumberBox.Text.Length);
                        tools.AssingPartNumber(tools.selectedElement, partNumber);
                        tools.count += 1;
                        tools.writeConfig(this.PrefixBox.Text, tools.count.ToString().PadLeft(this.NumberBox.Text.Length, '0'));

                        AssingPartNumberT.Commit();
                        
                        if(tools.count.ToString().Length == 1)
                            this.NumberBox.Text = "0" + tools.count.ToString();
                        else
                            this.NumberBox.Text = tools.count.ToString();
                    }
                }
                catch (System.Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

            }
            else { MessageBox.Show("Number field should contain only numbers"); }
            
            this.ShowDialog();
        }

        private void DiplaceUp_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            using (Autodesk.Revit.DB.Transaction DisplaceUp = new Autodesk.Revit.DB.Transaction(tools.uidoc.Document, "Displace Up Part Number"))
            {
                DisplaceUp.Start();
                tools.SetElementsUpStream();
                DisplaceUp.Commit();
            }

            this.ShowDialog();
        }


        private void DiplaceDn_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
            using (Autodesk.Revit.DB.Transaction DisplaceUp = new Autodesk.Revit.DB.Transaction(tools.uidoc.Document, "Displace Up Part Number"))
            {
                DisplaceUp.Start();
                tools.SetElementsDnStream();
                DisplaceUp.Commit();
            }

            this.ShowDialog();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            tools.resetValues();
        }
    }


}
