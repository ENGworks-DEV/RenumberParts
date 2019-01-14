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

            string oriFile = app.SharedParametersFilename;
            //Replace current sharedParamters with addin copy
            string assemblylocation = Assembly.GetExecutingAssembly().Location;
            string tempFile = new FileInfo(assemblylocation).Directory.FullName + @"\SP.txt";

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

            Autodesk.Revit.DB.Binding binding = app.Create.NewTypeBinding(cats);
            if (inst) binding = app.Create.NewInstanceBinding(cats);

            BindingMap map = (new UIApplication(app)).ActiveUIDocument.Document.ParameterBindings;
            map.Insert(def, binding, group);
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

        public static void AddToSelection()
        {

            var refElement = uidoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new SelectionFilter());

            if (refElement != null)
            {
                var element = uidoc.Document.GetElement(refElement);

                OverrideGraphicSettings overrideGraphicSettings = new OverrideGraphicSettings();
                overrideGraphicSettings.SetProjectionFillColor(new Color(255, 0, 0));
                overrideGraphicSettings.SetProjectionLineColor(new Color(255, 0, 0));


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

        public static string createNumbering(string prefix, int number, int chars)
        {

            var num = number.ToString().PadLeft(chars, '0');
            return prefix + num;

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

        public static void resetView()
        {
            using (Transaction ResetView = new Transaction(tools.uidoc.Document, "Reset view"))
            {
                ResetView.Start();
                OverrideGraphicSettings overrideGraphicSettings = new OverrideGraphicSettings();
                foreach (var item in ListOfElements)
                {
                    doc.ActiveView.SetElementOverrides(item.Id, overrideGraphicSettings);
                }

                ResetView.Commit();
            }
        }

        public static void resetValues()
        {
            using (Transaction ResetView = new Transaction(tools.uidoc.Document, "Reset view"))
            {
                ResetView.Start();
                OverrideGraphicSettings overrideGraphicSettings = new OverrideGraphicSettings();
                Guid guid = new Guid("460e0a79-a970-4b03-95f1-ac395c070beb");
                string blankPrtnmbr = "";
                foreach (var item in ListOfElements)
                {
                    
                    item.get_Parameter(guid).Set(blankPrtnmbr);
                }

                ResetView.Commit();
            }
        }


        public static void writeConfig(string prefix, string number)
        {

            var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\renumberConf";

            using (StreamWriter st = new StreamWriter(path))
            {
                st.WriteLine(prefix);
                st.WriteLine(number);

            }
        }

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
                                        var newnumber = createNumbering(itemNumber.Item1, currentNumber + 1, itemNumber.Item2.Count());

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
                                        var newnumber = createNumbering(itemNumber.Item1, currentNumber - 1, itemNumber.Item2.Count());

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

        public static Tuple<string, string> GetNumberAndPrexif(string CNumber)
        {
            var str = CNumber;

            if (str == null || !str.Any(char.IsDigit)) return null;
            StringBuilder sb = new StringBuilder();

            foreach (var c in str.ToCharArray().Reverse())
            {

                if (Regex.IsMatch(c.ToString(), @"\d"))
                {
                    sb.Append(c);

                }
                else
                {

                    break;
                }
            }

            string Number = new string(sb.ToString().Reverse().ToArray());
            string prefix = CNumber.Replace(Number, "");
            var tuple = new Tuple<string, string>(prefix, Number);

            return tuple;


        }
    }
}