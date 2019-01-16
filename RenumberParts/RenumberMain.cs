using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Drawing;
using Color = System.Drawing.Color;

namespace RenumberParts
{

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class RenumberMain : IExternalCommand
    {
        static AddInId appId = new AddInId(new Guid("3256F49C-7F76-4734-8992-3F1CF468BE9B"));

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //Define Uiapp and current document
            tools.uiapp = commandData.Application;
            tools.uidoc = tools.uiapp.ActiveUIDocument;
            tools.doc = tools.uidoc.Document;

            //Create project parameter from existing shared parameter
            using (Transaction t = new Transaction(tools.doc, "set Shared Parameters"))
            {
                t.Start();
                tools.RawCreateProjectParameterFromExistingSharedParameter(tools.doc.Application, "Item Number", tools.CategorySetList(), BuiltInParameterGroup.PG_IDENTITY_DATA, true);
                t.Commit();
            }

            //Create an instance of the MainForm.xaml
            var mainForm = new MainForm();
            Process process = Process.GetCurrentProcess();
            
            var h = process.MainWindowHandle;

            //Show MainForm.xaml on top of any other forms
            mainForm.Topmost = true;

            //Show the WPF MainForm.xaml
            mainForm.ShowDialog();

            return 0;

        }


    }



    public static class tools
    {
        public static Transaction Transaction { get; set; }
        private static string currentNumber;

        public static UIApplication uiapp { get; set; }

        public static UIDocument uidoc { get; set; }

        public static Document doc { get; set; }

        public static void RawCreateProjectParameterFromExistingSharedParameter(Autodesk.Revit.ApplicationServices.Application app, string name, CategorySet cats, BuiltInParameterGroup group, bool inst)
        {
            //Location of the shared parameters
            string oriFile = app.SharedParametersFilename;

            //My Documents
            var newpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            //Create txt inside my documents
            Extract("RenumberParts", newpath, "Resources", "SP.txt");

            //Create variable with the location on SP.txt inside my documents
            string tempFile = newpath + @"\SP.txt";

            //Change the location of the shared parameters for the SP location
            app.SharedParametersFilename = tempFile;
            DefinitionFile defFile = app.OpenSharedParameterFile();

            var v = (from DefinitionGroup dg in defFile.Groups
                     from ExternalDefinition d in dg.Definitions
                     where d.Name == name
                     select d);


            if (v == null || v.Count() < 1) throw new Exception("Invalid Name Input!wwwww");

            ExternalDefinition def = v.First();

            //Place original SP file 
            app.SharedParametersFilename = oriFile;

            //Delete SP temporary file
            System.IO.File.Delete(tempFile);

            Autodesk.Revit.DB.Binding binding = app.Create.NewTypeBinding(cats);
            if (inst) binding = app.Create.NewInstanceBinding(cats);

            BindingMap map = (new UIApplication(app)).ActiveUIDocument.Document.ParameterBindings;
            map.Insert(def, binding, group);
        }

        private static void Extract(string nameSpace, string outDirectory, string internalFilePath, string resourceName)
        {
            //Method to copy an embedded resource to a directory
            Assembly assembly = Assembly.GetCallingAssembly();

            using (Stream s = assembly.GetManifestResourceStream(nameSpace + "." + (internalFilePath == "" ? "" : internalFilePath + ".") + resourceName))
            using (BinaryReader r = new BinaryReader(s))
            using (FileStream fs = new FileStream(outDirectory + "\\" + resourceName, FileMode.OpenOrCreate))
            using (BinaryWriter w = new BinaryWriter(fs))
                w.Write(r.ReadBytes((int)s.Length));
        }


        public static void RawCreateProjectParameter(Autodesk.Revit.ApplicationServices.Application app, string name, ParameterType type, bool visible, CategorySet cats, BuiltInParameterGroup group, bool inst)
        {
            //InternalDefinition def = new InternalDefinition();
            //Definition def = new Definition();

            string oriFile = app.SharedParametersFilename;
            string tempFile = Path.GetTempFileName() + ".txt";
            using (File.Create(tempFile)) { }
            app.SharedParametersFilename = tempFile;

            ExternalDefinitionCreationOptions externalDefinitionCreationOptions = new ExternalDefinitionCreationOptions(name, type);

            ExternalDefinition def = app.OpenSharedParameterFile().Groups.Create("TemporaryDefintionGroup").Definitions.Create(externalDefinitionCreationOptions) as ExternalDefinition;

            app.SharedParametersFilename = oriFile;
            File.Delete(tempFile);

            Autodesk.Revit.DB.Binding binding = app.Create.NewTypeBinding(cats);
            if (inst) binding = app.Create.NewInstanceBinding(cats);

            BindingMap map = (new UIApplication(app)).ActiveUIDocument.Document.ParameterBindings;
            map.Insert(def, binding, group);
        }

        /// <summary>
        /// Categories used when creating shared parameters / project parameters
        /// </summary>
        /// <returns></returns>
        public static CategorySet CategorySetList()
        {
            CategorySet categorySet = new CategorySet();
            foreach (Category c in doc.Settings.Categories)
            {
                if (c.CategoryType == CategoryType.Model
                    && c.AllowsBoundParameters
                    )
                { categorySet.Insert(c); }

            }

            return categorySet;
        }

        public static List<Element> ListOfElements { get; set; } = new List<Element>();

        public static Element selectedElement { get; set; }


        /// <summary>
        /// Save an element on a temporary list and override its color
        /// </summary>
        public static void AddToSelection()
        {

            var refElement = uidoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new SelectionFilter());

            if (refElement != null)
            {
                var element = uidoc.Document.GetElement(refElement);

                OverrideGraphicSettings overrideGraphicSettings = new OverrideGraphicSettings();
                Color colorSelect = MainForm.ColorSelected;

                byte r = 255;
                byte b = 0;
                byte g = 0;

                if (colorSelect != null)
                {
                    r = colorSelect.R;
                    g = colorSelect.G;
                    b = colorSelect.B;
                }
                
                overrideGraphicSettings.SetProjectionFillColor(new Autodesk.Revit.DB.Color(r, g, b));
                overrideGraphicSettings.SetProjectionLineColor(new Autodesk.Revit.DB.Color(r, g, b));


                doc.ActiveView.SetElementOverrides(element.Id, overrideGraphicSettings);
                selectedElement = element;
                ListOfElements.Add(element);

            }

        }

        /// <summary>
        /// Get selected elements
        /// </summary>
        /// <returns></returns>
        public static List<Element> getSelection()
        {
            var output = new List<Element>();
            var selection = uidoc.Selection.GetElementIds();
            foreach (var item in selection)
            {

                try
                {
                    Element element = doc.GetElement(item);
                    output.Add(element);
                }
                catch (Exception e) { MessageBox.Show(e.Message); }

            }

            return output;
        }

        /// <summary>
        /// Get number from element as string
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static string getNumber(Element element)
        {

            currentNumber = "";

            if (element.Category.Name.Contains("MEP"))
            {
                try
                {

                    var value = element.get_Parameter(BuiltInParameter.FABRICATION_PART_ITEM_NUMBER).AsString();

                    currentNumber = value;
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }

            else
            {
                Guid guid = new Guid("460e0a79-a970-4b03-95f1-ac395c070beb");
                var value = element.get_Parameter(guid).AsString();

                currentNumber = value;
            }
            return currentNumber;
        }

        /// <summary>
        /// Creates a new number from a prefix, separator, a number and the amount of digits the number should have
        /// </summary>
        /// <param name="prefix">Prefix</param>
        /// <param name="separator">Separator string</param>
        /// <param name="number">Current number</param>
        /// <param name="chars">Amount of digits the number should have</param>
        /// <returns></returns>
        public static string createNumbering(string prefix,string separator, int number, int chars)
        {

            var num = number.ToString().PadLeft(chars, '0');
            return prefix + separator + num;

        }

        public static string ActualCounter { get; set; }

        public static int count { get; set; }

        public class SelectionFilter : ISelectionFilter
        {
            #region ISelectionFilter Members

            public bool AllowElement(Element elem)
            {


                if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_FabricationDuctwork) return true;
                if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_FabricationPipework) return true;
                if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeAccessory) return true;
                if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeCurves) return true;
                if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeFitting) return true;
                if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_DuctAccessory) return true;
                if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_DuctCurves) return true;
                if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_DuctFitting) return true;

                return false;
            }

            public bool AllowReference(Reference refer, XYZ pos)
            {
                return false;
            }

            #endregion
        }

        /// <summary>
        /// Resets override on all elements in current view
        /// </summary>
        public static void resetView()
        {
            using (Transaction ResetView = new Transaction(tools.uidoc.Document, "Reset view"))
            {
                ResetView.Start();
                OverrideGraphicSettings overrideGraphicSettings = new OverrideGraphicSettings();
                
                LogicalOrFilter logicalOrFilter = new LogicalOrFilter(filters());

                var collector = new FilteredElementCollector(doc, doc.ActiveView.Id).WherePasses(
                    logicalOrFilter).WhereElementIsNotElementType();

                foreach (var item in collector.ToElements())
                {
                    if (item.IsValidObject && doc.GetElement(item.Id) != null)
                    { 
                    doc.ActiveView.SetElementOverrides(item.Id, overrideGraphicSettings);
                    }
                    
                }

                ResetView.Commit();
            }
        }

        /// <summary>
        /// Reset prefix values on all elements visible on current view
        /// </summary>
        public static void resetValues()
        {
            LogicalOrFilter logicalOrFilter = new LogicalOrFilter(filters());

            var collector = new FilteredElementCollector(doc, doc.ActiveView.Id).WherePasses(
                logicalOrFilter).WhereElementIsNotElementType();
            using (Transaction ResetView = new Transaction(tools.uidoc.Document, "Reset view"))
            {
                ResetView.Start();
                OverrideGraphicSettings overrideGraphicSettings = new OverrideGraphicSettings();
                Guid guid = new Guid("460e0a79-a970-4b03-95f1-ac395c070beb");
                string blankPrtnmbr = "";
                foreach (var item in collector.ToElements())
                {
                    
                    item.get_Parameter(guid).Set(blankPrtnmbr);
                }

                ResetView.Commit();
            }
        }

        /// <summary>
        /// Write configuration file saved on user document 
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="number"></param>
        public static void writeConfig(string prefix, string number)
        {

            var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\renumberConf";

            using (StreamWriter st = new StreamWriter(path))
            {
                st.WriteLine(prefix);
                st.WriteLine(number);

            }
        }


        /// <summary>
        /// Read configuration file saved on user document 
        /// </summary>
        /// <returns></returns>
        public static List<string> readConfig()
        {
            var output = new List<string>();
            var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\renumberConf";
            if (File.Exists(path))
            {
                using (StreamReader st = new StreamReader(path))
                {
                    int counter = 0;
                    string line;
                    while ((line = st.ReadLine()) != null)
                    {
                        output.Add(line);
                        counter++;
                    }


                }
            }
            return output;
        }


        /// <summary>
        /// Assing number to element
        /// </summary>
        /// <param name="element"></param>
        /// <param name="partNumber"></param>
        public static void AssingPartNumber(Element element, string partNumber)
        {


            //Check if element is Fab part or common mep element
            if (element.Category.Name.Contains("MEP"))
            {
                try
                {

                    element.get_Parameter(BuiltInParameter.FABRICATION_PART_ITEM_NUMBER).Set(partNumber);
                    ListOfElements.Add(element);
                    ActualCounter = partNumber;
                    uidoc.RefreshActiveView();
                    uidoc.Selection.SetElementIds(new List<ElementId>() { element.Id });
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }

            else
            {
                Guid guid = new Guid("460e0a79-a970-4b03-95f1-ac395c070beb");
                element.get_Parameter(guid).Set(partNumber);
                ListOfElements.Add(element);
                uidoc.Selection.SetElementIds(new List<ElementId>() { element.Id });
            }

        }

        /// <summary>
        /// Hard coded list of categories to be used as a filter, only pipes, ducts and FabParts should be included when selecting and filter
        /// </summary>
        /// <returns></returns>
        public static List<ElementFilter> filters()
        {
            var output = new List<ElementFilter>();
            output.Add(new ElementCategoryFilter(BuiltInCategory.OST_FabricationDuctwork));
            output.Add(new ElementCategoryFilter(BuiltInCategory.OST_FabricationPipework));
            output.Add(new ElementCategoryFilter(BuiltInCategory.OST_PipeAccessory));
            output.Add(new ElementCategoryFilter(BuiltInCategory.OST_PipeCurves));
            output.Add(new ElementCategoryFilter(BuiltInCategory.OST_PipeFitting));
            output.Add(new ElementCategoryFilter(BuiltInCategory.OST_DuctAccessory));
            output.Add(new ElementCategoryFilter(BuiltInCategory.OST_DuctCurves));
            output.Add(new ElementCategoryFilter(BuiltInCategory.OST_DuctFitting));
            return output;
        }

        /// <summary>
        /// Adds 1 to all elements with the same prefix and bigger number than the one on selection
        /// </summary>
        public static void SetElementsUpStream()
        {



            tools.AddToSelection();
            if (tools.selectedElement != null)
            {
                Element element = tools.selectedElement;


                LogicalOrFilter logicalOrFilter = new LogicalOrFilter(filters());

                var collector = new FilteredElementCollector(doc, doc.ActiveView.Id).WherePasses(logicalOrFilter).WhereElementIsNotElementType();
                var startNumber = getNumber(element);
                var limit = GetNumberAndPrexif(startNumber);
                if (limit != null)
                {
                    foreach (var item in collector.ToElements())
                    {
                        var number = getNumber(item);
                        if (number != null)
                        {
                            if (number.Contains(limit.Item1))
                            {
                                var itemNumber = GetNumberAndPrexif(number);
                                if (itemNumber != null)
                                {
                                    var limitNumber = int.Parse(limit.Item2);
                                    var currentNumber = int.Parse(itemNumber.Item2);

                                    if (limitNumber < currentNumber)
                                    {
                                        var newnumber = createNumbering(itemNumber.Item1,MainForm.Separator, currentNumber + 1, itemNumber.Item2.Count());

                                        AssingPartNumber(item, newnumber);

                                    }
                                }

                            }
                        }

                    }
                }


            }

            else
            {
                MessageBox.Show("Please select an element");
            }

        }

        /// <summary>
        /// Subtract 1 to all elements with the same prefix and bigger number than the one on selection
        /// </summary>
        internal static void SetElementsDnStream()
        {
            tools.AddToSelection();
            if (tools.selectedElement != null)
            {
                Element element = tools.selectedElement;

                LogicalOrFilter logicalOrFilter = new LogicalOrFilter(filters());

                var collector = new FilteredElementCollector(doc, doc.ActiveView.Id).WherePasses(logicalOrFilter).WhereElementIsNotElementType();
                var startNumber = getNumber(element);
                var limit = GetNumberAndPrexif(startNumber);
                if (limit != null)
                {
                    foreach (var item in collector.ToElements())
                    {
                        var number = getNumber(item);
                        if (number != null)
                        {
                            if (number.Contains(limit.Item1))
                            {
                                var itemNumber = GetNumberAndPrexif(number);
                                if (itemNumber != null)
                                {
                                    var limitNumber = int.Parse(limit.Item2);
                                    var currentNumber = int.Parse(itemNumber.Item2);

                                    if (limitNumber < currentNumber)
                                    {
                                        var newnumber = createNumbering(itemNumber.Item1,MainForm.Separator, currentNumber - 1, itemNumber.Item2.Count());

                                        AssingPartNumber(item, newnumber);

                                    }
                                }

                            }
                        }

                    }
                }


            }

            else
            {
                MessageBox.Show("Please select an element");
            }
        }
        

        /// <summary>
        /// Get number and prefix from string using the separator
        /// </summary>
        /// <param name="CNumber"></param>
        /// <returns></returns>
        public static Tuple<string, string> GetNumberAndPrexif(string CNumber)
        {
            var str = CNumber;
            var separator = MainForm.Separator;
            var PrefixValueArray = CNumber.Split(new[] { separator }, StringSplitOptions.None);

            //Just last object should be a number, if there is no number place 000
            int c;
            bool isNumeric = int.TryParse(PrefixValueArray.Last(), out c);
            string Number = "000";
            if (isNumeric)
            {
                Number =  PrefixValueArray.Last();
            }
            //Remove the separator and number leaving the prefix only
            string prefix = CNumber.Replace(separator+Number, "");
            var tuple = new Tuple<string, string>(prefix, Number);

            return tuple;


        }
    }
}