
#region Namespaces
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Resources;
using System.Diagnostics;
using RenumberParts.Model;

#endregion

namespace RenumberParts
{
    class App : IExternalApplication
    {

        public Result OnStartup(UIControlledApplication application)
        {

            // Get the absolut path of this assembly
            string ExecutingAssemblyPath = System.Reflection.Assembly.GetExecutingAssembly(
                ).Location;

            // Create a ribbon panel
            RibbonPanel m_projectPanel = application.CreateRibbonPanel(
                "RenumberParts");
            
            //Button
            PushButton pushButton = m_projectPanel.AddItem(new PushButtonData(
                "RenumberParts", "Renumber Parts",ExecutingAssemblyPath,
                "RenumberParts.RenumberMain")) as PushButton;

            //Add Help ToolTip 
            pushButton.ToolTip = "RenumberParts";

            //Add long description 
            pushButton.LongDescription =
             "This addin helps you to renumber MEP part with a prefix";

            // Set the large image shown on button.
            pushButton.LargeImage = PngImageSource(
                "RenumberParts.Resources.RenumberPartsLogo.png");

            // Get the location of the solution DLL
            string path = System.IO.Path.GetDirectoryName(
               System.Reflection.Assembly.GetExecutingAssembly().Location);

            // Combine path with \
            string newpath = Path.GetFullPath(Path.Combine(path, @"..\"));


            ContextualHelp contextHelp = new ContextualHelp(
                ContextualHelpType.Url,
                "https://engworks.com/renumber-parts/");

            // Assign contextual help to pushbutton
            pushButton.SetContextualHelp(contextHelp);

            return Result.Succeeded;

        }

        private System.Windows.Media.ImageSource PngImageSource(string embeddedPath)
        {
            // Get Bitmap from Resources folder
            Stream stream = this.GetType().Assembly.GetManifestResourceStream(embeddedPath);
            var decoder = new System.Windows.Media.Imaging.PngBitmapDecoder(stream,
                BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            return decoder.Frames[0];
        }

        public Result OnShutdown(UIControlledApplication a)
        {
            return Result.Succeeded;
        }
    }


    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class RenumberMain : IExternalCommand
    {
        static AddInId appId = new AddInId(new Guid("3256F49C-7F76-4734-8992-3F1CF468BE9B"));

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //Define Uiapp and current document
            tools.uiapp = commandData.Application;
            tools.uidoc = tools.uiapp.ActiveUIDocument;
            tools.doc = tools.uidoc.Document;

            //Create project parameter from existing shared parameter
            using (Transaction t = new Transaction(tools.doc, "set Shared Parameters"))
            {
                t.Start();
                tools.RawCreateProjectParameterFromExistingSharedParameter(tools.doc.Application, 
                    "Item Number", ShareParamCategories.listCat(), BuiltInParameterGroup.PG_IDENTITY_DATA, true);
                t.Commit();
            }

            //Create an instance of the MainForm.xaml
            var mainForm = new MainForm(commandData);
            Process process = Process.GetCurrentProcess();

            var h = process.MainWindowHandle;

            //Show MainForm.xaml on top of any other forms
            mainForm.Topmost = true;

            //Show the WPF MainForm.xaml
            mainForm.ShowDialog();

            return 0;

        }

    }
}
