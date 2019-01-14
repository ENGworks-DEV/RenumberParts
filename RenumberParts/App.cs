
#region Namespaces
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion

namespace RenumberParts
{
    class App : IExternalApplication
    {

        private System.Windows.Media.ImageSource PngImageSource(string embeddedPath)
        {
            Stream stream = this.GetType().Assembly.GetManifestResourceStream(embeddedPath);
            var decoder = new System.Windows.Media.Imaging.PngBitmapDecoder(stream,
                BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            return decoder.Frames[0];
        }

        public Result OnStartup(UIControlledApplication application)
        {
            // Get the absolut path of this assembly
            string ExecutingAssemblyPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

            // Create a ribbon panel
            RibbonPanel m_projectPanel = application.CreateRibbonPanel("RenumberParts");
            
            //Button
            PushButton pushButton = m_projectPanel.AddItem(new PushButtonData("RenumberParts", "Renumber Parts",
               ExecutingAssemblyPath, "RenumberParts.RenumberMain")) as PushButton;

            //Add Help ToolTip 
            pushButton.ToolTip = "RenumberParts";

            //Add long description 
            pushButton.LongDescription =
             "This addin helps you to renumber MEP part with a prefix";

            // Set the large image shown on button.
            pushButton.LargeImage = PngImageSource("RenumberParts.Resources.RenumberPartsLogo.png");

            // Context (F1) Help
            string path;
            path = System.IO.Path.GetDirectoryName(
               System.Reflection.Assembly.GetExecutingAssembly().Location);

            string newpath = Path.GetFullPath(Path.Combine(path, @"..\"));

            ContextualHelp contextHelp = new ContextualHelp(
                ContextualHelpType.ChmFile,
                newpath + "Resources\\Help.html"); 

            pushButton.SetContextualHelp(contextHelp);






            return Result.Succeeded;
        }


        public Result OnShutdown(UIControlledApplication a)
        {
            return Result.Succeeded;
        }
    }
}
