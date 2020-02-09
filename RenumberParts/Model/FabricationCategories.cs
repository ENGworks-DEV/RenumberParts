using Autodesk.Revit.DB;
using System.Collections.Generic;

namespace RenumberParts.Model
{
    
    public static class FabCategories
    {
        /// <summary>
        /// Hard coded list of Fabricationparts categories 
        /// to be used as a filter between fabrication and 
        /// non fabrication parts
        /// </summary>
        /// <returns></returns>
        public static List<BuiltInCategory> listCat()
        {
            var output = new List<BuiltInCategory>();
            output.Add((BuiltInCategory.OST_FabricationDuctwork));
            output.Add(BuiltInCategory.OST_FabricationPipework);
            output.Add(BuiltInCategory.OST_FabricationContainment);
            return output;
        }
    }
}
