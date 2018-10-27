#region Namespaces
using System;
using System.Collections.Generic;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Reflection;
#endregion

namespace ClosestGridsAddin
{
    class App : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication a)
        {
            RibbonPanel Ribbon_Panel;
            PushButtonData Cmd_1;


            a.CreateRibbonTab("Automation1");
            Ribbon_Panel = a.CreateRibbonPanel("Automation1", "Attach Grids");

            Cmd_1 = new PushButtonData(@"Cmd_AttachGrid", "Attach Grids", Assembly.GetExecutingAssembly().Location, "ClosestGridsAddin.Command");
            if (Ribbon_Panel != null)
            {
                Ribbon_Panel.AddItem(Cmd_1);
            }


            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication a)
        {
            return Result.Succeeded;
        }
    }
}
