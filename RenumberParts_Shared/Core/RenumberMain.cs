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
using System.Timers;
using RenumberParts.Model;

namespace RenumberParts
{
    public static class tools
    {
        public static Transaction Transaction { get; set; }
        private static string currentNumber;

        public static UIApplication uiapp { get; set; }

        public static UIDocument uidoc { get; set; }
        /// <summary>
        /// current document
        /// </summary>
        public static Document doc { get; set; }



        /// <summary>
        /// Create project parameter from existing shared parameter
        /// </summary>
        /// <param name="app"></param>
        /// <param name="name"></param>
        /// <param name="cats"></param>
        /// <param name="group"></param>
        /// <param name="inst"></param>
        public static void RawCreateProjectParameterFromExistingSharedParameter(Autodesk.Revit.ApplicationServices.Application app, string name, CategorySet cats, BuiltInParameterGroup group, bool inst)
        {
            //Location of the shared parameters
            string oriFile = app.SharedParametersFilename;

            //My Documents
            var newpath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            //Create txt inside my documents
            Extract("RenumberParts", newpath, "Resources", "SP_RenumberParts.txt");

            //Create variable with the location on SP.txt inside my documents
            string tempFile = newpath + @"\SP_RenumberParts.txt";

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


        /// <summary>
        /// Copy and embedded resource to a directory(txt file in this case)
        /// </summary>
        /// <param name="nameSpace"></param>
        /// <param name="outDirectory"></param>
        /// <param name="internalFilePath"></param>
        /// <param name="resourceName"></param>
        private static void Extract(string nameSpace, string outDirectory, string internalFilePath, string resourceName)
        {
            Assembly assembly = Assembly.GetCallingAssembly();

            using (Stream s = assembly.GetManifestResourceStream(nameSpace + "." + (internalFilePath == "" ? "" : internalFilePath + ".") + resourceName))
            using (BinaryReader r = new BinaryReader(s))
            using (FileStream fs = new FileStream(outDirectory + "\\" + resourceName, FileMode.OpenOrCreate))
            using (BinaryWriter w = new BinaryWriter(fs))
                w.Write(r.ReadBytes((int)s.Length));
        }


        /// <summary>
        /// Create a project parameter
        /// </summary>
        /// <param name="app"></param>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="visible"></param>
        /// <param name="cats"></param>
        /// <param name="group"></param>
        /// <param name="inst"></param>
        public static void RawCreateProjectParameter(Autodesk.Revit.ApplicationServices.Application app, string name, ParameterType type, bool visible, CategorySet cats, BuiltInParameterGroup group, bool inst)
        {


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

        public static List<Element> ListOfElements { get; set; } = new List<Element>();

        public static Element selectedElement { get; set; }

        public static List<Element> selectedElements { get; set; } = new List<Element>();


        /// <summary>
        /// Save an element on a temporary list and override its color
        /// </summary>
        public static void AddToSelection()
        {
            selectedElements.Clear();

            var filterS = new SelectionFilter();
            
            var refElement = uidoc.Selection.PickObject(Autodesk.Revit.UI.Selection.ObjectType.Element, new SelectionFilter());

            if (refElement != null)
            {
                var element = uidoc.Document.GetElement(refElement);


                selectedElements.Add(element);

                //TODO: check if element is from the right category
                OverrideGraphicSettings overrideGraphicSettings = new OverrideGraphicSettings();
                Color colorSelect = MainForm.ColorSelected;

                //Split coloSelect in R,G,B to be transformed to a Revit color later
                byte r = colorSelect.R;
                byte g = colorSelect.G;
                byte b = colorSelect.B;


                #if REVIT2020
                OverrideElemtColor.Graphics20192020(doc,ref overrideGraphicSettings, r, g, b);
                #elif REVIT2019
                OverrideElemtColor.Graphics20172020(doc,ref overrideGraphicSettings, r, g, b);
                #endif


                foreach (Element x in selectedElements)
                { 
                //Override color of element
                doc.ActiveView.SetElementOverrides(x.Id, overrideGraphicSettings);
                }

                selectedElement = element;

                //Add element to the temporary list of selected elemenents
                ListOfElements.Add(element);

            }

        }

        /// <summary>
        /// Save an element on a temporary list and override its color
        /// </summary>
        public static void AddToSelection(ElementId ReferenceElem, List<Element> completeList)
        {

                foreach (Element elem in completeList)
                {
                selectedElements.Add(elem);
                //Category cat = elem.Category;
                //    BuiltInCategory enumCat = (BuiltInCategory)cat.Id.IntegerValue;

                //    if (getNumber(elem) == "" && elem.get_Parameter(BuiltInParameter.FABRICATION_PART_DEPTH_IN) != null)
                //    {


                //        if (filterParam(ReferenceElem, elem, BuiltInParameter.ELEM_FAMILY_PARAM,
                //            BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM, BuiltInParameter.FABRICATION_PART_DEPTH_IN,
                //            BuiltInParameter.FABRICATION_PART_WIDTH_IN, BuiltInParameter.FABRICATION_SERVICE_PARAM,
                //            BuiltInParameter.FABRICATION_PART_LENGTH))
                //        {
                //            selectedElements.Add(elem);
                //        }
                //    }

                }
                    selectedElements.Add(tools.doc.GetElement(ReferenceElem));

            OverrideGraphicSettings overrideGraphicSettings = new OverrideGraphicSettings();
            System.Drawing.Color colorSelect = MainForm.ColorSelected;
            byte r = colorSelect.R;
            byte b = colorSelect.B;
            byte g = colorSelect.G;
            //overrideGraphicSettings.SetProjectionFillColor(new Autodesk.Revit.DB.Color(r, g, b));
            overrideGraphicSettings.SetProjectionLineColor(new Autodesk.Revit.DB.Color(r, g, b));
            foreach (Element x in tools.selectedElements)
            {
                tools.doc.ActiveView.SetElementOverrides(x.Id, overrideGraphicSettings);
            }
            //Category category = ReferenceElem.Category;
            //BuiltInCategory enumCategory = (BuiltInCategory)category.Id.IntegerValue;
            //BuiltInCategory builtCategory = (BuiltInCategory)Enum.Parse(typeof(BuiltInCategory), ReferenceElem.Category.Id.ToString());

            //foreach (Element elem in completeList)
            //{
            //    Category cat = elem.Category;
            //    BuiltInCategory enumCat = (BuiltInCategory)cat.Id.IntegerValue;

            //    if (//enumCat.ToString() == "OST_FabricationDuctwork"
            //        &&
            //        getNumber(elem) == "" && elem.get_Parameter(BuiltInParameter.FABRICATION_PART_DEPTH_IN) != null)
            //    {


            //        if (filterParam(ReferenceElem, elem, BuiltInParameter.ELEM_FAMILY_PARAM,
            //            BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM, BuiltInParameter.FABRICATION_PART_DEPTH_IN,
            //            BuiltInParameter.FABRICATION_PART_WIDTH_IN, BuiltInParameter.FABRICATION_SERVICE_PARAM,
            //            BuiltInParameter.FABRICATION_PART_LENGTH))
            //        {
            //            selectedElements.Add(elem);
            //        }
            //    }
            //    ListOfElements.Add(elem);

            //}
            //if (!selectedElements.Contains(ReferenceElem) && tools.getNumber(ReferenceElem) == "")
            //{
            //    selectedElements.Add(ReferenceElem);
            //}


            //OverrideGraphicSettings overrideGraphicSettings = new OverrideGraphicSettings();
            //System.Drawing.Color colorSelect = MainForm.ColorSelected;
            //byte r = colorSelect.R;
            //byte b = colorSelect.B;
            //byte g = colorSelect.G;
            ////overrideGraphicSettings.SetProjectionFillColor(new Autodesk.Revit.DB.Color(r, g, b));
            //overrideGraphicSettings.SetProjectionLineColor(new Autodesk.Revit.DB.Color(r, g, b));
            //foreach (Element x in tools.selectedElements)
            //{
            //    tools.doc.ActiveView.SetElementOverrides(x.Id, overrideGraphicSettings);
            //}
            //var element = ReferenceElem;


            //selectedElements = completeList;

            //TODO: check if element is from the right category
            //OverrideGraphicSettings overrideGraphicSettings = new OverrideGraphicSettings();
            //Color colorSelect = MainForm.ColorSelected;

            ////Split coloSelect in R,G,B to be transformed to a Revit color later
            //byte r = colorSelect.R;
            //byte g = colorSelect.G;
            //byte b = colorSelect.B;


#if REVIT2020
                OverrideElemtColor.Graphics20192020(doc,ref overrideGraphicSettings, r, g, b);
#elif REVIT2019
            OverrideElemtColor.Graphics20172020(doc, ref overrideGraphicSettings, r, g, b);
#endif


            foreach (Element x in selectedElements)
            {
                //Override color of element
                doc.ActiveView.SetElementOverrides(x.Id, overrideGraphicSettings);
            }

        }


        public static bool filterParam(Element elementEx, Element elementExP, BuiltInParameter param01, BuiltInParameter param02,
            BuiltInParameter param03, BuiltInParameter param04, BuiltInParameter param05, BuiltInParameter param06)
        {
            bool result = false;

            string elemParam06 = "n";

            string elemParam01 = elementEx.get_Parameter(param01).AsValueString();
            string elemParam02 = elementEx.get_Parameter(param02).AsValueString();
            string elemParam03 = elementEx.get_Parameter(param03).AsValueString();
            string elemParam04 = elementEx.get_Parameter(param04).AsValueString();
            string elemParam05 = elementEx.get_Parameter(param05).AsValueString();
            try
            {
                elemParam06 = elementEx.get_Parameter(param06).AsValueString();
            }
            catch
            {

            }
            string elemParam06P = "";

            string elemParam01P = elementExP.get_Parameter(param01).AsValueString();
            string elemParam02P = elementExP.get_Parameter(param02).AsValueString();
            string elemParam03P = elementExP.get_Parameter(param03).AsValueString();
            string elemParam04P = elementExP.get_Parameter(param04).AsValueString();
            string elemParam05P = elementExP.get_Parameter(param05).AsValueString();
            try
            {
                elemParam06P = elementExP.get_Parameter(param06).AsValueString();
            }
            catch
            {

            }


            if (elemParam01 == elemParam01P)
            {
                if (elemParam02 == elemParam02P)
                {
                    if (elemParam03 == elemParam03P)
                    {
                        if (elemParam04 == elemParam04P)
                        {
                            if (elemParam05 == elemParam05P)
                            {
                                if (elemParam06 == elemParam06P)
                                {
                                    result = true;
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        public static void filterParam(Element elementEx, Autodesk.Revit.DB.BuiltInCategory Cat, BuiltInParameter param01, BuiltInParameter param02,
            BuiltInParameter param03, BuiltInParameter param04, BuiltInParameter param05)
        {
            
            string elemParam01 = elementEx.get_Parameter(param01).AsValueString();
            string elemParam02 = elementEx.get_Parameter(param02).AsValueString();
            string elemParam03 = elementEx.get_Parameter(param03).AsValueString();
            string elemParam04 = elementEx.get_Parameter(param04).AsValueString();
            string elemParam05 = elementEx.get_Parameter(param05).AsValueString();

            FilteredElementCollector viewCollector = new FilteredElementCollector(doc, uidoc.ActiveView.Id);
            List<Element> ducts = new FilteredElementCollector(doc, uidoc.ActiveView.Id)
                .OfCategory(Cat)
                .Where(a => a.get_Parameter(param01).AsValueString() == elemParam01)
                .Where(a => a.get_Parameter(param02).AsValueString() == elemParam02)
                .Where(a => a.get_Parameter(param03).AsValueString() == elemParam03)
                .Where(a => a.get_Parameter(param04).AsValueString() == elemParam04)
                .Where(a => a.get_Parameter(param05).AsValueString() == elemParam05)
                .ToList();

            foreach (Element x in ducts)
            {
                selectedElements.Add(x);
                ListOfElements.Add(x);
            }
        }


        /// <summary>
        /// Get number from element as string
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static string getNumber(Element element)
        {

            currentNumber = "";

            Category category = element.Category;
            BuiltInCategory enumCategory = (BuiltInCategory)category.Id.IntegerValue;
            
            List<BuiltInCategory> allBuiltinCategories = FabCategories.listCat();

            if (allBuiltinCategories.Contains(enumCategory))
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
                if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_FabricationContainment) return true;
                if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeAccessory) return true;
                if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeCurves) return true;
                if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_PipeFitting) return true;
                if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_DuctAccessory) return true;
                if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_DuctCurves) return true;
                if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_DuctFitting) return true;
                if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Conduit) return true;
                if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_ConduitFitting) return true;
                if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_CableTray) return true;
                if (elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_CableTrayFitting) return true;

                return false;
            }

            public bool AllowReference(Reference refer, XYZ pos)
            {
                return false;
            }

#endregion
        }


        /// <summary>
        /// Write configuration file saved on user document 
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="number"></param>
        public static void writeConfig(string prefix,string separator, string number)
        {

            var path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + @"\renumberConf";

            using (StreamWriter st = new StreamWriter(path))
            {
                st.WriteLine(prefix);
                st.WriteLine(separator);
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

            Category category = element.Category;
            BuiltInCategory enumCategory = (BuiltInCategory)category.Id.IntegerValue;     
            List<BuiltInCategory> allBuiltinCategories = FabCategories.listCat();

            if (allBuiltinCategories.Contains(enumCategory))
            {
                try
                {
                    element.get_Parameter(BuiltInParameter.FABRICATION_PART_ITEM_NUMBER).Set(partNumber);
                    ListOfElements.Add(element);
                    ActualCounter = partNumber;
                    uidoc.RefreshActiveView();
                    //uidoc.Selection.SetElementIds(new List<ElementId>() { element.Id });
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }

            else
            {
                Guid guid = new Guid("460e0a79-a970-4b03-95f1-ac395c070beb");
                element.get_Parameter(guid).Set(partNumber);
                ListOfElements.Add(element);
                //uidoc.Selection.SetElementIds(new List<ElementId>() { element.Id });
            }

        }

      
        /// <summary>
        /// Adds 1 to all elements with the same prefix and bigger number than the one on selection
        /// </summary>
        public static void SetElementsUpStream()
        {

            tools.AddToSelection();
            if (tools.selectedElement != null)
            {
                //Get the selected element
                Element element = tools.selectedElement;

                //Get Shared parameter with the Prefix and number
                var startNumber = getNumber(element);

                //Get Number and prefix of the selected element
                var limit = GetNumberAndPrexif(startNumber);

                //Get filter with all the MEP categories
                LogicalOrFilter logicalOrFilter = new LogicalOrFilter(MEPCategories.listCat());

                //Get all MEP elements in active view
                var collector = new FilteredElementCollector(doc, doc.ActiveView.Id).WherePasses(logicalOrFilter).WhereElementIsNotElementType();

                

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
                                        var newnumber = createNumbering(itemNumber.Item1, MainForm.Separator, currentNumber + 1, itemNumber.Item2.Count());

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

                LogicalOrFilter logicalOrFilter = new LogicalOrFilter(MEPCategories.listCat());

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
            if (!string.IsNullOrEmpty(CNumber))
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
                    Number = PrefixValueArray.Last();
                }
                //Remove the separator and number leaving the prefix only
                string prefix = CNumber.Replace(separator + Number, "");
                var tuple = new Tuple<string, string>(prefix, Number);

                return tuple;

            }
            else { return null; }
        }

        public static Element NewSelection()
        {
            tools.selectedElements.Clear();
            tools.SelectionFilter filterS = new tools.SelectionFilter();
            try
            {
                Reference refElement = tools.uidoc.Selection.PickObject(ObjectType.Element, new tools.SelectionFilter());
                return tools.doc.GetElement(refElement);
            }
            catch
            {
                return null;
            }

        }
    }
}