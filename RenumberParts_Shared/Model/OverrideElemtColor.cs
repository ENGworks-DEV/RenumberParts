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

namespace RenumberParts.Model
{
    public static class OverrideElemtColor
    {

        public static void Graphics20192020(Document doc, ref OverrideGraphicSettings overrideGraphicSettings, byte r, byte g, byte b)
        {
#if REVIT2020
            var patternCollector = new FilteredElementCollector(doc);
            patternCollector.OfClass(typeof(FillPatternElement));
            FillPatternElement fpe = patternCollector.ToElements()
                .Cast<FillPatternElement>().First(x => x.GetFillPattern().Name == "<Solid fill>");
            overrideGraphicSettings.SetSurfaceForegroundPatternId(fpe.Id);
            overrideGraphicSettings.SetSurfaceForegroundPatternVisible(true);
            overrideGraphicSettings.SetSurfaceForegroundPatternColor(new Autodesk.Revit.DB.Color(r, g, b));
            overrideGraphicSettings.SetProjectionLineColor(new Autodesk.Revit.DB.Color(r, g, b));
#endif    
        }

        public static void Graphics20172020(Document doc, ref OverrideGraphicSettings overrideGraphicSettings, byte r, byte g, byte b)
        {
#if REVIT2019
            var patternCollector = new FilteredElementCollector(doc);
            patternCollector.OfClass(typeof(FillPatternElement));
            FillPatternElement fpe = patternCollector.ToElements()
                .Cast<FillPatternElement>().First(x => x.GetFillPattern().Name == "Solid fill");
            overrideGraphicSettings.SetProjectionFillPatternId(fpe.Id);
            overrideGraphicSettings.SetProjectionFillPatternVisible(true);
            overrideGraphicSettings.SetProjectionFillColor(new Autodesk.Revit.DB.Color(r, g, b));
            overrideGraphicSettings.SetProjectionLineColor(new Autodesk.Revit.DB.Color(r, g, b));
#endif
        }
    }
}
