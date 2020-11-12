using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;

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

                var selection = tools.uidoc.Selection.GetElementIds();
                List<ElementId> collector = new List<ElementId>();
                if (selection != null && selection.Count > 0)
                {
                    foreach (var item in selection)
                    {
                        collector.Add(item);
                    }
                }

                else
                {
                    LogicalOrFilter logicalOrFilter = new LogicalOrFilter(MEPCategories.listCat());
                    collector = new FilteredElementCollector(tools.doc, tools.doc.ActiveView.Id).WherePasses(
                        logicalOrFilter).WhereElementIsNotElementType().ToElementIds().ToList();
                }


                foreach (var item in collector)
                {
                    if (tools.doc.GetElement(item) != null)
                    {
                        tools.doc.ActiveView.SetElementOverrides(item, overrideGraphicSettings);
                    }

                }

                ResetView.Commit();
            }
        }
    }
}
