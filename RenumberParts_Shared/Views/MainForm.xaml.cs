
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
    public partial class MainForm : Window
    {
        static List<ElementId> elemIds = new List<ElementId>();

        public static System.Drawing.Color ColorSelected;
        public ColorDialog colorDialog = new ColorDialog();
        private bool checkBoxDuplicates;
        public bool RoundDuctsBool = true;

        public bool SnglElmtButton { get; private set; }

        private ExternalCommandData p_commanddata;

        public Document _doc;

        public UIApplication uiApp;

        ColorForm colorForm = new ColorForm();

        public MainForm(ExternalCommandData cmddata_p)
        {

            DataContext = this;
            InitializeComponent();
            var conf = tools.readConfig();
            DataContext = this;
            p_commanddata = cmddata_p;
            InitializeComponent();
            uiApp = cmddata_p.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            _doc = uiDoc.Document;

            //Set as default red color
            ColorSelected = System.Drawing.Color.FromArgb(1, 255, 0, 0);


            if (conf != null && conf.Count == 3)
            {
                PrefixBox.Text = conf[0];
                SeparatorBox.Text = conf[1];
                NumberBox.Text = conf[2];
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
            NumberBox.Select(NumberBox.Text.Length, 0);
            int c;
            bool isNumeric = int.TryParse(NumberBox.Text, out c);

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
        private void ColorOverride_Button(object sender, RoutedEventArgs e)
        {
            ConfirmDelete confirmDeleteForm = new ConfirmDelete("Are you sure you want to reset" + Environment.NewLine + "all color override?");
            confirmDeleteForm.Topmost = true;
            confirmDeleteForm.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            var result = confirmDeleteForm.ShowDialog();

            if ((bool)result)
            {
                ResetColors.reset();
            }
        }



        internal static string Separator { get; set; }

        /// <summary>
        /// Add spool number to element
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ManualButton_Click(object sender, RoutedEventArgs e)
        {

            System.Drawing.Color color = ColorSelected;

            Hide();
            int c;
            bool isNumeric = int.TryParse(NumberBox.Text, out c);

            if (isNumeric)
            {
                if (tools.count < int.Parse(NumberBox.Text))
                {
                    tools.count = c;
                }

                try
                {
                    using (Transaction AssingPartNumberT = new Transaction(tools.uidoc.Document, "Assing Part Number"))
                    {
                        AssingPartNumberT.Start();

                        tools.AddToSelection(checkBoxDuplicates);

                        var partNumber = tools.createNumbering(PrefixBox.Text, SeparatorBox.Text, tools.count, NumberBox.Text.Length);


                        //tools.AssingPartNumber(tools.selectedElement, partNumber);

                        foreach (Element x in tools.selectedElements)
                        {
                            tools.AssingPartNumber(x, partNumber);
                        }


                        tools.count += 1;
                        //count 5, pad left

                        int leadingZeros = NumberBox.Text.Length > 1 ? NumberBox.Text.Length - tools.count.ToString().Length : 0;
                        NumberBox.Text = (new string('0', leadingZeros)) + tools.count.ToString();

                        tools.writeConfig(PrefixBox.Text, SeparatorBox.Text, NumberBox.Text);

                        AssingPartNumberT.Commit();



                    }
                }
                catch (Exception ex)
                {
                    ex = null;
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

            ShowDialog();
        }

        /// <summary>
        /// Add spool number to elements
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AutoButton_Click(object sender, RoutedEventArgs e)
        {
            List<Element> RunElements = new List<Element>();
            List<ElementId> RunElementsId = new List<ElementId>();
            Hide();
            Element elem;


            //Select the element only if it is a part number
            using (Transaction AssingPartNumberT = new Transaction(tools.uidoc.Document, "Assing Part Number"))
            {
                AssingPartNumberT.Start();
                elem = tools.NewSelection();
                AssingPartNumberT.Commit();
            }

            //Make sure that the user selected a valid element
            if (elem == null)
            {
                goto CloseApp;
            }
            elemIds = new List<ElementId>();

            elemIds.Add(elem.Id);

            int countElem = getElementConnected(ref elem);
            if (countElem > 1)
            {
                elemIds.RemoveAt(elemIds.Count() - 1);
                System.Windows.MessageBox.Show("The element has more than one connection path", "warning");
            }

            while (countElem == 1)
            {
                countElem = getElementConnected(ref elem);
            }

            if (elemIds.Count != 0)
            {
                foreach (ElementId elmnt in elemIds)
                {
                    using (Transaction AssingPartNumberT2 = new Transaction(tools.uidoc.Document, "Assing Part Number"))
                    {
                        tools.selectedElements.Clear();
                        AssingPartNumberT2.Start();


                        //Find all the elements that are the same in the RunElements list
                        tools.AddToSelection(elmnt, RunElements);

                        //Generate the part number
                        string partNumber = tools.createNumbering(PrefixBox.Text, SeparatorBox.Text, tools.count, NumberBox.Text.Length);

                        //Assign the part number to each selected element
                        foreach (Element x in tools.selectedElements)
                        {
                            tools.AssingPartNumber(x, partNumber);
                        }


                        if (tools.selectedElements.Count != 0)
                        {
                            tools.count++;
                        }

                        int leadingZeros = (NumberBox.Text.Length > 1) ? (NumberBox.Text.Length - tools.count.ToString().Length) : 0;

                        if (leadingZeros >= 0)
                        {
                            NumberBox.Text = new string('0', leadingZeros) + tools.count.ToString();
                        }
                        else
                        {
                            NumberBox.Text = tools.count.ToString();
                        }

                        tools.writeConfig(PrefixBox.Text, SeparatorBox.Text, NumberBox.Text);
                        Options options = uiApp.Application.Create.NewGeometryOptions();
                        AssingPartNumberT2.Commit();
                    }
                }
            }

        CloseApp:;
            ShowDialog();

        }


        private List<string> listToString(List<Element> RunElements)
        {
            List<string> listToString = new List<string>();
            List<string> elementString = new List<string>();
            foreach (Element elem in RunElements)
            {
                foreach (Parameter param in elem.GetOrderedParameters())
                {
                    elementString.Add(param.Definition.Name.ToString() + param.AsValueString());
                }
                string elementParams = string.Join(", ", elementString.ToArray());
                listToString.Add(elementParams);
            }
            return listToString;
        }

        private int ConnectionCheck(Connector con)
        {
            List<string> categorias = new List<string>(new string[] { "OST_CableTrayFitting", "OST_CableTray", "OST_ConduitFitting", "OST_Conduit", "OST_DuctFitting", "OST_DuctCurves", "OST_DuctAccessory", "OST_FabricationParts", "OST_FabricationPipework", "OST_FabricationHangers", "OST_PipeAccessory", "OST_PipeFitting", "OST_PipeCurves", "OST_FabricationDuctwork" });
            int tempInt = 0;
            ConnectorSet secondary = con.AllRefs;
            ConnectorSet TempConnectors = null;

            foreach (Connector f in secondary)
            {
                Element tempElem = _doc.GetElement(f.Owner.Id);
                Category cat = tempElem.Category;
                BuiltInCategory enumCat = (BuiltInCategory)cat.Id.IntegerValue;

                if (categorias.Contains(enumCat.ToString()))
                {
                    TempConnectors = getConnectorSetFromElement(tempElem);
                    foreach (Connector connec in TempConnectors)
                    {
                        if (MainForm.CloseEnoughForMe(connec.Origin.X, con.Origin.X) &&
                            MainForm.CloseEnoughForMe(connec.Origin.Y, con.Origin.Y) &&
                            MainForm.CloseEnoughForMe(connec.Origin.Z, con.Origin.Z))
                        {
                            tempInt = 1;
                        }
                    }
                }
            }

            if (TempConnectors != null)
            {
            }

            return tempInt;
        }

        private static bool CloseEnoughForMe(double value1, double value2)
        {
            return Math.Abs(value1 - value2) <= 1.0;
        }


        /// <summary>
        /// Get the current conector of the element
        /// </summary>
        /// <param name="elm"></param>
        /// <param name="conn"></param>
        private void CheckDoubleConn(ref int checkValue, ref ConnectorSet connectSet, ref Connector CurrentConnector)
        {
            List<Connector> connectorList = new List<Connector>();
            List<ElementId> adjacentElement = new List<ElementId>();


            foreach (Connector c in connectSet)
            {
                if (ConnectorsHelper.ConnStatus(c))
                {
                    ConnectorSet secondary = c.AllRefs;
                    foreach (Connector cone in secondary)
                    {
                        if (!adjacentElement.Contains(cone.Owner.Id))
                            adjacentElement.Add(cone.Owner.Id);
                    }
                }
            }

            //adjacentElement = adjacentElement.Distinct<Element>().ToList<Element>();
            Connector tempConnector = null;
            int count = 0;

            foreach (ElementId elemId in adjacentElement)
            {
                Element elem = _doc.GetElement(elemId);

                if (string.IsNullOrEmpty(elem.LookupParameter("Item Number").AsValueString()))
                {
                    foreach (Connector c3 in connectorList)
                    {
                        if (isAdjacentValidElmt(elem, c3))
                        {
                            count++;
                            tempConnector = c3;
                        }
                    }
                }
            }

            if (count == 1)
            {
                checkValue = 1;
                CurrentConnector = tempConnector;
            }
        }

        /// <summary>
        /// Check if the adjacent element is valid
        /// </summary>
        /// <param name="elm"></param>
        /// <param name="conn"></param>
        private bool isAdjacentValidElmt(Element elm, Connector conn)
        {
            bool validation = false;
            List<string> originConnectors = new List<string>();
            ConnectorSet TempConnectors = getConnectorSetFromElement(elm);
            foreach (Connector connec in TempConnectors)
            {
                if (connec.IsConnected && connec.ConnectorType.ToString() != "Curve")
                {
                    originConnectors.Add(connec.Origin.ToString());
                }
            }
            if (originConnectors.Contains(conn.Origin.ToString()))
            {
                validation = true;
            }
            return validation;
        }

        /// <summary>
        /// Get The connector set of an element
        /// </summary>
        /// <param name="elem"></param>
        private ConnectorSet getConnectorSetFromElement(Element elem)
        {
            ConnectorSet connectors = null;
            //elemIds.Add(elem.Id);
            bool flag = elem is FamilyInstance;
            if (flag)
            {
                MEPModel i = ((FamilyInstance)elem).MEPModel;
                connectors = i.ConnectorManager.Connectors;
            }
            else
            {
                bool flag2 = elem is FabricationPart;
                if (flag2)
                {
                    connectors = ((FabricationPart)elem).ConnectorManager.Connectors;
                }
                else
                {
                    bool flag3 = elem is MEPCurve;
                    if (flag3)
                    {
                        connectors = ((MEPCurve)elem).ConnectorManager.Connectors;
                    }
                    else
                    {
                        Debug.Assert(elem.GetType().IsSubclassOf(typeof(MEPCurve)), "expected all candidate connector provider elements to be either family instances or derived from MEPCurve");
                    }
                }
            }
            //foreach (Connector cone in connectors)
            //{
            //    if (!elemIds.Contains(cone.Owner.Id))
            //        elemIds.Add(cone.Owner.Id);
            //}

            ConnectorSet tempConnectors = new ConnectorSet();
            foreach (Connector con in connectors)
            {
                if (!elemIds.Contains(con.Owner.Id))
                {
                    elemIds.Add(con.Owner.Id);
                    Element tempElem = _doc.GetElement(con.Owner.Id);

                    if (string.IsNullOrEmpty(tempElem.LookupParameter("Item Number").AsString()))
                    {
                        tempConnectors.Insert(con);
                    }
                }
            }
            return tempConnectors;
        }

        /// <summary>
        /// Get The connector set of an element
        /// </summary>
        /// <param name="elem"></param>
        private int getElementConnected(ref Element elem)
        {
            List<string> categories = new List<string>(new string[] { "OST_CableTrayFitting", "OST_CableTray", "OST_ConduitFitting", "OST_Conduit", "OST_DuctFitting", "OST_DuctCurves", "OST_DuctAccessory", "OST_FabricationParts", "OST_FabricationPipework", "OST_FabricationHangers", "OST_PipeAccessory", "OST_PipeFitting", "OST_PipeCurves", "OST_FabricationDuctwork" });
            ConnectorSet connectors = null;
            int countElemToContinue = 0;
            //elemIds.Add(elem.Id);
            bool flag = elem is FamilyInstance;
            if (flag)
            {
                MEPModel i = ((FamilyInstance)elem).MEPModel;
                connectors = i.ConnectorManager.Connectors;
            }
            else
            {
                bool flag2 = elem is FabricationPart;
                if (flag2)
                {
                    connectors = ((FabricationPart)elem).ConnectorManager.Connectors;
                }
                else
                {
                    bool flag3 = elem is MEPCurve;
                    if (flag3)
                    {
                        connectors = ((MEPCurve)elem).ConnectorManager.Connectors;
                    }
                    else
                    {
                        Debug.Assert(elem.GetType().IsSubclassOf(typeof(MEPCurve)), "expected all candidate connector provider elements to be either family instances or derived from MEPCurve");
                    }
                }
            }
            //foreach (Connector cone in connectors)
            //{
            //    if (!elemIds.Contains(cone.Owner.Id))
            //        elemIds.Add(cone.Owner.Id);
            //}

            ConnectorSet tempConnectors = new ConnectorSet();
            foreach (Connector connec in connectors)
            {
                foreach (Connector con in connec.AllRefs)
                {
                    if (!elemIds.Contains(con.Owner.Id))
                    {
                        Element tempElem = _doc.GetElement(con.Owner.Id);
                        Category cat = tempElem.Category;
                        BuiltInCategory enumCat = (BuiltInCategory)cat.Id.IntegerValue;

                        if (categories.Contains(enumCat.ToString()))
                        {

                            if (con.Owner.Id != elem.Id && string.IsNullOrEmpty(tempElem.LookupParameter("Item Number").AsString()))
                            {
                                if (countElemToContinue == 0)
                                    elemIds.Add(con.Owner.Id);
                                else
                                   if (countElemToContinue == 1)
                                    elemIds.RemoveAt(elemIds.Count() - 1);

                                countElemToContinue++;
                                tempConnectors.Insert(con);
                                elem = con.Owner;
                            }
                        }
                    }
                }
            }
            return countElemToContinue;
        }

        /// <summary>
        /// if checkbox round ducts is checked filter round ducts
        /// </summary>
        bool checkRoundDucts(bool checkBox, Element elm)
        {
            bool result = false;

            if (checkBox)
            {
                result = true;
            }
            else
            {
                if (elm.LookupParameter("Main Primary Diameter") != null)
                {

                    result = false;
                }
                else
                {
                    result = true;
                }
            }
            return result;
        }

        /// <summary>
        /// Displace all values up
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DiplaceUp_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            try
            {
                using (Transaction DisplaceUp = new Transaction(tools.uidoc.Document, "Displace Up Part Number"))
                {
                    DisplaceUp.Start();
                    tools.SetElementsUpStream(checkBoxDuplicates);
                    DisplaceUp.Commit();
                }
            }
            catch
            {

            }

            ShowDialog();
        }



        /// <summary>
        /// Desplace all values down
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DiplaceDn_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            try
            {
                using (Transaction DisplaceUp = new Transaction(tools.uidoc.Document, "Displace Up Part Number"))
                {
                    DisplaceUp.Start();
                    tools.SetElementsDnStream(checkBoxDuplicates);
                    DisplaceUp.Commit();
                }

            }
            catch
            {

            }
            ShowDialog();
        }


        /// <summary>
        /// Create an instance of the confirmDeleteForm.xaml
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResetValues_Button(object sender, RoutedEventArgs e)
        {
            ConfirmDelete confirmDeleteForm = new ConfirmDelete("Are you sure you want to delete" + Environment.NewLine + "all values?");
            confirmDeleteForm.Topmost = true;
            confirmDeleteForm.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            var result = confirmDeleteForm.ShowDialog();

            if ((bool)result)
            {
                ResetValues.reset();
            }

        }

        /// <summary>
        /// Show Color dialog and set selected color to variable
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            ColorForm.colorSelected = System.Windows.Media.Color.FromArgb(ColorSelected.A, ColorSelected.R, ColorSelected.G, ColorSelected.B);
            Hide();
            colorForm = new ColorForm();
            colorForm.ShowDialog();
            if ((bool)colorForm.DialogResult.Value)
            {
                ColorSelected = System.Drawing.Color.FromArgb(ColorForm.colorSelected.A, ColorForm.colorSelected.R, ColorForm.colorSelected.G, ColorForm.colorSelected.B);
                ShowDialog();
            }
            ColorSelected = colorDialog.Color;
        }

        public void SeparatorBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SeparatorBox.Select(SeparatorBox.Text.Length, 0);
            Separator = SeparatorBox.Text;
        }

        private void Border_MouseDown(object sender, MouseButtonEventArgs e)

        {
            DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }


        private void Title_Link(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://engworks.com/renumber-parts/");
        }

        private void PrefixBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            PrefixBox.Select(PrefixBox.Text.Length, 0);
        }

        /// <summary>
        /// Button close form when clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            MainFormSettings SettingsForm = new MainFormSettings(p_commanddata);
            SettingsForm.Topmost = true;
            SettingsForm.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            SettingsForm.ShowDialog();

            checkBoxDuplicates = SettingsForm.checkBoxDuplicates.IsChecked.Value;
            //RoundDuctsBool = SettingsForm.Round_Ducts.IsChecked.Value;
            //SnglElmtButton = SettingsForm.SnglElmtButton.IsChecked.Value;
        }
    }
}
