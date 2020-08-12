using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Windows.Documents;

namespace RenumberParts.Model
{
    public static class ShareParamCategories
    {
        /// <summary>
        /// Categories used when creating shared parameters / project parameters
        /// </summary>
        /// <returns></returns>
        public static CategorySet listCat()
        {
            CategorySet categorySet = new CategorySet();

            foreach (Category c in tools.doc.Settings.Categories)
            {
                if (c.CategoryType == CategoryType.Model
                    && c.AllowsBoundParameters
                    && !FabCategories.listCat().Contains((BuiltInCategory)c.Id.IntegerValue)
                    )
                { categorySet.Insert(c); }

            }

            return categorySet;
        }
    }
}
