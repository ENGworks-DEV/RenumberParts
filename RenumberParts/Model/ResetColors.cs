using Autodesk.Revit.DB;


namespace RenumberParts.Model
{
    public static class ResetColors
    {
        /// <summary>
        /// Resets override on all elements in current view
        /// </summary>
        public static void reset()
        {
            using (Transaction ResetView = new Transaction(tools.uidoc.Document, "Reset view"))
            {
                ResetView.Start();
                OverrideGraphicSettings overrideGraphicSettings = new OverrideGraphicSettings();

                LogicalOrFilter logicalOrFilter = new LogicalOrFilter(MEPCategories.listCat());

                var collector = new FilteredElementCollector(tools.doc, tools.doc.ActiveView.Id).WherePasses(
                    logicalOrFilter).WhereElementIsNotElementType();

                foreach (var item in collector.ToElements())
                {
                    if (item.IsValidObject && tools.doc.GetElement(item.Id) != null)
                    {
                        tools.doc.ActiveView.SetElementOverrides(item.Id, overrideGraphicSettings);
                    }

                }

                ResetView.Commit();
            }
        }
    }
}
