using Autodesk.Revit.DB;
using System.Collections.Generic;


namespace RenumberParts.Model
{
    public static class MEPCategories
    {
        /// <summary>
        /// Hard coded list of categories to be used as a filter, only pipes, ducts and FabParts should be included when selecting and filter
        /// </summary>
        /// <returns></returns>
        public static List<ElementFilter> listCat()
        {
            var output = new List<ElementFilter>();
            output.Add(new ElementCategoryFilter(BuiltInCategory.OST_FabricationDuctwork));
            output.Add(new ElementCategoryFilter(BuiltInCategory.OST_FabricationPipework));
            output.Add(new ElementCategoryFilter(BuiltInCategory.OST_FabricationContainment));
            output.Add(new ElementCategoryFilter(BuiltInCategory.OST_PipeAccessory));
            output.Add(new ElementCategoryFilter(BuiltInCategory.OST_PipeCurves));
            output.Add(new ElementCategoryFilter(BuiltInCategory.OST_PipeFitting));
            output.Add(new ElementCategoryFilter(BuiltInCategory.OST_DuctAccessory));
            output.Add(new ElementCategoryFilter(BuiltInCategory.OST_DuctCurves));
            output.Add(new ElementCategoryFilter(BuiltInCategory.OST_DuctFitting));
            output.Add(new ElementCategoryFilter(BuiltInCategory.OST_Conduit));
            output.Add(new ElementCategoryFilter(BuiltInCategory.OST_ConduitFitting));
            output.Add(new ElementCategoryFilter(BuiltInCategory.OST_CableTray));
            output.Add(new ElementCategoryFilter(BuiltInCategory.OST_CableTrayFitting));
            return output;
        }
    }
}
