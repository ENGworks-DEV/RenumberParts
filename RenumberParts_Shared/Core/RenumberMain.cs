﻿using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using RenumberParts.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using Color = System.Drawing.Color;

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
        public static void AddToSelection(bool duplicated)
        {
            selectedElements.Clear();

            var filterS = new SelectionFilter();

            var refElement = uidoc.Selection.PickObject(ObjectType.Element, new SelectionFilter());

            if (refElement != null)
            {
                var element = uidoc.Document.GetElement(refElement);

                if (duplicated)
                {
                    selectedElements.AddRange(GetallElementsDuplicated(element));
                }
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
                OverrideElemtColor.Graphics20172020(doc, ref overrideGraphicSettings, r, g, b);
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

        private static List<Element> GetallElementsDuplicated(Element element)
        {
            var collector = new FilteredElementCollector(doc, doc.ActiveView.Id).OfCategoryId(element.Category.Id)
                .WhereElementIsNotElementType()
                .ToElements();

            var r = new List<Element>();
            switch (element.Category.Id.IntegerValue)
            {

                case (int)BuiltInCategory.OST_FabricationDuctwork:
                    r = collector.Where(x => CheckFabDuctEquality(element, x)).ToList();
                    break;
                case (int)BuiltInCategory.OST_FabricationPipework:
                    r = collector.Where(x => CheckFabPipeEquality(element, x)).ToList();
                    break;
                case (int)BuiltInCategory.OST_DuctCurves:
                    r = collector.Where(x => CheckDuctEquality(element, x)).ToList();
                    break;
                case (int)BuiltInCategory.OST_PipeCurves:
                    r = collector.Where(x => CheckPipesEquality(element, x)).ToList();
                    break;
                case (int)BuiltInCategory.OST_Conduit:
                    r = collector.Where(x => CheckConduitEquality(element, x)).ToList();
                    break;
                case (int)BuiltInCategory.OST_CableTray:
                    r = collector.Where(x => CheckCableTrayEquality(element, x)).ToList();
                    break;
                default:
                    break;
            }

            return r;
        }

        private static bool CheckCableTrayEquality(Element element, Element x)
        {
            bool fam = element.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString()
                == x.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString();

            bool system = false;
            if (element.get_Parameter(BuiltInParameter.RBS_CTC_SERVICE_TYPE) != null &&
                x.get_Parameter(BuiltInParameter.RBS_CTC_SERVICE_TYPE) != null)
            {
                system = element.get_Parameter(BuiltInParameter.RBS_CTC_SERVICE_TYPE).AsValueString()
                    == x.get_Parameter(BuiltInParameter.RBS_CTC_SERVICE_TYPE).AsValueString();
            }

            bool size = element.get_Parameter(BuiltInParameter.RBS_CALCULATED_SIZE).AsValueString()
                == x.get_Parameter(BuiltInParameter.RBS_CALCULATED_SIZE).AsValueString();

            bool length = element.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsValueString()
                == x.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsValueString();

            return (fam && system && size && length);
        }

        private static bool CheckConduitEquality(Element element, Element x)
        {
            bool fam = element.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString()
                == x.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString();

            bool system = false;
            if (element.get_Parameter(BuiltInParameter.RBS_CTC_SERVICE_TYPE) != null &&
                x.get_Parameter(BuiltInParameter.RBS_CTC_SERVICE_TYPE) != null)
            {
                system = element.get_Parameter(BuiltInParameter.RBS_CTC_SERVICE_TYPE).AsValueString()
                    == x.get_Parameter(BuiltInParameter.RBS_CTC_SERVICE_TYPE).AsValueString();
            }

            bool size = element.get_Parameter(BuiltInParameter.RBS_CALCULATED_SIZE).AsValueString()
                == x.get_Parameter(BuiltInParameter.RBS_CALCULATED_SIZE).AsValueString();

            bool length = element.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsValueString()
                == x.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsValueString();

            return (fam && system && size && length);
        }

        private static bool CheckDuctEquality(Element element, Element x)
        {
            bool fam = element.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString()
                == x.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString();

            bool system = false;
            if (element.get_Parameter(BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM) != null &&
                x.get_Parameter(BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM) != null)
            {
                system = element.get_Parameter(BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM).AsValueString()
                    == x.get_Parameter(BuiltInParameter.RBS_DUCT_SYSTEM_TYPE_PARAM).AsValueString();
            }

            bool size = element.get_Parameter(BuiltInParameter.RBS_CALCULATED_SIZE).AsValueString()
                == x.get_Parameter(BuiltInParameter.RBS_CALCULATED_SIZE).AsValueString();

            bool length = element.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsValueString()
                == x.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsValueString();

            return (fam && system && size && length);
        }

        private static bool CheckPipesEquality(Element element, Element x)
        {
            bool fam = element.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString()
                == x.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString();

            bool system = false;
            if (element.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM) != null &&
                x.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM) != null)
            {
                system = element.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM).AsValueString()
                    == x.get_Parameter(BuiltInParameter.RBS_PIPING_SYSTEM_TYPE_PARAM).AsValueString();
            }

            bool size = element.get_Parameter(BuiltInParameter.RBS_CALCULATED_SIZE).AsValueString()
                == x.get_Parameter(BuiltInParameter.RBS_CALCULATED_SIZE).AsValueString();

            bool length = element.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsValueString()
                == x.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsValueString();

            return (fam && system && size && length);
        }

        private static bool CheckFabDuctEquality(Element element, Element x)
        {

            bool fam = element.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString() 
                == x.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString();

            bool system = false;
            if (element.get_Parameter(BuiltInParameter.FABRICATION_SERVICE_PARAM) != null &&
                x.get_Parameter(BuiltInParameter.FABRICATION_SERVICE_PARAM) != null)
            {
                system = element.get_Parameter(BuiltInParameter.FABRICATION_SERVICE_PARAM).AsValueString() 
                    == x.get_Parameter(BuiltInParameter.FABRICATION_SERVICE_PARAM).AsValueString();
            }
            
            bool depth = element.get_Parameter(BuiltInParameter.FABRICATION_PART_DEPTH_IN).AsValueString() 
                == x.get_Parameter(BuiltInParameter.FABRICATION_PART_DEPTH_IN).AsValueString();

            bool width = element.get_Parameter(BuiltInParameter.FABRICATION_PART_WIDTH_IN).AsValueString() 
                == x.get_Parameter(BuiltInParameter.FABRICATION_PART_WIDTH_IN).AsValueString();

            bool length = element.get_Parameter(BuiltInParameter.FABRICATION_PART_LENGTH).AsValueString() 
                == x.get_Parameter(BuiltInParameter.FABRICATION_PART_LENGTH).AsValueString();

            return (fam && system && depth && width && length);
        }

        private static bool CheckFabPipeEquality(Element element, Element x)
        {

            bool fam = element.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString()
                == x.get_Parameter(BuiltInParameter.ELEM_FAMILY_AND_TYPE_PARAM).AsValueString();

            bool system = false;
            if (element.get_Parameter(BuiltInParameter.FABRICATION_SERVICE_PARAM) != null &&
                x.get_Parameter(BuiltInParameter.FABRICATION_SERVICE_PARAM) != null)
            {
                system = element.get_Parameter(BuiltInParameter.FABRICATION_SERVICE_PARAM).AsValueString()
                    == x.get_Parameter(BuiltInParameter.FABRICATION_SERVICE_PARAM).AsValueString();
            }

            bool diamenter = element.get_Parameter(BuiltInParameter.FABRICATION_PART_DIAMETER_IN).AsValueString()
                == x.get_Parameter(BuiltInParameter.FABRICATION_PART_DIAMETER_IN).AsValueString();

            bool diameterOut = element.get_Parameter(BuiltInParameter.FABRICATION_PART_DIAMETER_OUT).AsValueString()
                == x.get_Parameter(BuiltInParameter.FABRICATION_PART_DIAMETER_OUT).AsValueString();

            bool length = element.get_Parameter(BuiltInParameter.FABRICATION_PART_LENGTH).AsValueString()
                == x.get_Parameter(BuiltInParameter.FABRICATION_PART_LENGTH).AsValueString();

            return (fam && system && diamenter && diameterOut && length);
        }

        /// <summary>
        /// Save an element on a temporary list and override its color
        /// </summary>
        public static void AddToSelection(ElementId ReferenceElem, List<Element> completeList)
        {

            foreach (Element elem in completeList)
            {
                selectedElements.Add(elem);

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
        public static string createNumbering(string prefix, string separator, int number, int chars)
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
                if (elem.Category == null) return false;
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
        public static void writeConfig(string prefix, string separator, string number)
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
        public static void SetElementsUpStream(bool duplicated)
        {

            tools.AddToSelection(duplicated);
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
        internal static void SetElementsDnStream(bool duplicated)
        {
            tools.AddToSelection(duplicated);
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
                                        var newnumber = createNumbering(itemNumber.Item1, MainForm.Separator, currentNumber - 1, itemNumber.Item2.Count());

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