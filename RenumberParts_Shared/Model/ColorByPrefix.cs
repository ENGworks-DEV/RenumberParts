using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using Color = System.Drawing.Color;


namespace RenumberParts.Model
{
    public static class ColorByPrfx
    {
        /// <summary>
        /// Override colors in view by prefix
        /// </summary>
        public static void ColorInView()
        {
            using (Transaction ResetView = new Transaction(tools.uidoc.Document, "Reset view"))
            {
                ResetView.Start();
                OverrideGraphicSettings overrideGraphicSettings = new OverrideGraphicSettings();

                LogicalOrFilter logicalOrFilter = new LogicalOrFilter(MEPCategories.listCat());

                var collector = new FilteredElementCollector(tools.doc, tools.doc.ActiveView.Id).WherePasses(
                    logicalOrFilter).WhereElementIsNotElementType();

                Dictionary<Element, string> elemtNPrefix = new Dictionary<Element, string>();
                Dictionary<string, Color> colorNPrefix = new Dictionary<string, Color>();

                //Create a Dictionary with Elements and its prefixes
                foreach (var item in collector.ToElements())
                {
                    var number = tools.getNumber(item);
                    var itemNumber = tools.GetNumberAndPrexif(number);
                    if (itemNumber != null) { elemtNPrefix.Add(item, itemNumber.Item1); }

                }

                //Create a unique prefixes and an assigned color
                foreach (var prefix in elemtNPrefix.Values.Distinct())
                {
                    //Chanchada
                    System.Threading.Thread.Sleep(50);
                    Random randonGen = new Random();
                    Color randomColor = Color.FromArgb(1, randonGen.Next(255), randonGen.Next(255), randonGen.Next(255));
                    colorNPrefix.Add(prefix, randomColor);

                }

                //Override colors following the already created schema of colors
                foreach (var item in elemtNPrefix)
                {

                    #if REVIT2020 || REVIT2019
                    OverrideElemtColor.Graphics20192020(tools.uidoc.Document, ref overrideGraphicSettings, colorNPrefix[item.Value].R, colorNPrefix[item.Value].G, colorNPrefix[item.Value].B);
                    #else
                    OverrideElemtColor.Graphics20172020(tools.uidoc.Document, ref overrideGraphicSettings, colorNPrefix[item.Value].R, colorNPrefix[item.Value].G, colorNPrefix[item.Value].B);
                    #endif

                    tools.doc.ActiveView.SetElementOverrides(item.Key.Id, overrideGraphicSettings);

                }

                ResetView.Commit();
            }
        }
    }
}
