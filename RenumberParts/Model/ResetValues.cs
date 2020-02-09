using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;


namespace RenumberParts.Model
{
    public static class ResetValues
    {
        /// <summary>
        /// Reset prefix values on all elements visible on current view
        /// </summary>
        public static void reset()
        {
            List<BuiltInCategory> allBuiltinCategories = FabCategories.listCat();

            LogicalOrFilter logicalOrFilter = new LogicalOrFilter(MEPCategories.listCat());

            var collector = new FilteredElementCollector(tools.doc, tools.doc.ActiveView.Id).WherePasses(
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
                    Category category = item.Category;
                    BuiltInCategory enumCategory = (BuiltInCategory)category.Id.IntegerValue;
                    if (allBuiltinCategories.Contains(enumCategory))
                    {
                        item.get_Parameter(BuiltInParameter.FABRICATION_PART_ITEM_NUMBER).Set(blankPrtnmbr);
                    }
                }

                ResetView.Commit();

            }
        }
    }
}
