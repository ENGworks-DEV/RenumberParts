using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RenumberParts.Model
{
    public static class ResetValues
    {
        /// <summary>
        /// Reset prefix values on all elements visible on current view
        /// </summary>
        public static void reset()
        {

            var selection = tools.uidoc.Selection.GetElementIds();
            List<Element> collector = new List<Element>();
            if (selection != null && selection.Count > 0)
            {
                foreach (var item in selection)
                {
                    collector.Add(tools.uidoc.Document.GetElement(item));
                }
            }
            else
            {
                LogicalOrFilter logicalOrFilter = new LogicalOrFilter(MEPCategories.listCat());
                collector = new FilteredElementCollector(tools.doc, tools.doc.ActiveView.Id).WherePasses(
                    logicalOrFilter).WhereElementIsNotElementType().ToElements().ToList();
            }

            using (Transaction ResetView = new Transaction(tools.uidoc.Document, "Reset view"))
            {
                ResetView.Start();
                OverrideGraphicSettings overrideGraphicSettings = new OverrideGraphicSettings();
                Guid guid = new Guid("460e0a79-a970-4b03-95f1-ac395c070beb");
                string blankPrtnmbr = "";
                foreach (var item in collector)
                {
                    Parameter param= item.get_Parameter(guid);
                    if (param is null)
                    {
                        param = item.get_Parameter(BuiltInParameter.FABRICATION_PART_ITEM_NUMBER);
                    }
                    param.Set(blankPrtnmbr);
                }

                ResetView.Commit();

            }
        }
    }
}
