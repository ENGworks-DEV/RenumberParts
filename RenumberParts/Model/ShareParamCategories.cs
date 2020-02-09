using Autodesk.Revit.DB;


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
                    )
                { categorySet.Insert(c); }

            }

            return categorySet;
        }
    }
}
