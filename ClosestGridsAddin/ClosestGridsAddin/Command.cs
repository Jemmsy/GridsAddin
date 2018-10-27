#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Linq;
#endregion

namespace ClosestGridsAddin
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData,ref string message,ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;


            try
            {
                IList<Reference> Elements=  uidoc.Selection.PickObjects(ObjectType.Element,"Please Select the elements");

                Transaction tr = new Transaction(doc);
                tr.Start("Get Grids");

                FilteredElementCollector GridsCollector
                  = new FilteredElementCollector(doc)
                    .WhereElementIsNotElementType()
                    .OfCategory(BuiltInCategory.INVALID)
                    .OfClass(typeof(Grid));



                //List<Wall> walls = WallsCollector.Cast<Wall>().ToList();
                List<Grid> grids = GridsCollector.Cast<Grid>().ToList();
            //List<FamilyInstance> Beams = BeamsCollector.Cast<FamilyInstance>().ToList();
            List<Wall> walls =new List<Wall>();
            //List<Grid> grids = new List<Grid>();
            List<FamilyInstance> Beams = new List<FamilyInstance>();
            List<FamilyInstance> columns = new List<FamilyInstance>();

            foreach (var item in Elements)
            {
                Element e = doc.GetElement(item);
                if (e is Wall)
                {
                    walls.Add((e as Wall));
                }
                else if (e is FamilyInstance)
                {
                    Category beamcat = Category.GetCategory(doc, BuiltInCategory.OST_StructuralFraming);
                    Category columnscat = Category.GetCategory(doc, BuiltInCategory.OST_StructuralColumns);
                    Category Archcolumnscat = Category.GetCategory(doc, BuiltInCategory.OST_Columns);

                    if (e.Category.Name == beamcat.Name)
                    {
                        Beams.Add((FamilyInstance)e);
                    }
                    else if (e.Category.Name == columnscat.Name || e.Category.Name == Archcolumnscat.Name)
                    {
                        columns.Add((FamilyInstance)e);
                    }
                }
            }

            //FilteredElementCollector WallsCollector
            //  = new FilteredElementCollector(doc)
            //    .WhereElementIsNotElementType()
            //    .OfCategory(BuiltInCategory.INVALID)
            //    .OfClass(typeof(Wall));



            //FilteredElementCollector BeamsCollector
            //  = new FilteredElementCollector(doc)
            //    .WhereElementIsNotElementType()
            //    .OfCategory(BuiltInCategory.OST_StructuralFraming)
            //    .OfClass(typeof(FamilyInstance));


            //FilteredElementCollector ColumnsCollector
            //  = new FilteredElementCollector(doc)
            //    .WhereElementIsNotElementType()
            //    .OfCategory(BuiltInCategory.OST_StructuralColumns)
            //    .OfClass(typeof(FamilyInstance));

            foreach (var column in columns)
            {
                //string s=  column.get_Parameter(BuiltInParameter.COLUMN_LOCATION_MARK).AsString();
                //  s = s.Split('(')[0] + "-" + s.Split(')')[1].Split('(')[0];
                KeyValuePair<Grid, Grid> g_vert = GetClosest2Grids(((LocationPoint)column.Location).Point, column.LevelId, grids);
                string name = g_vert.Key.Name + "-" + g_vert.Value.Name;

                column.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set(name);
            }
            foreach (var wall in walls)
            {
                Curve l = ((LocationCurve)wall.Location).Curve;
                XYZ start = l.GetEndPoint(0);
                XYZ end = l.GetEndPoint(1);
                XYZ Center = (start + end) / 2;
                KeyValuePair<Grid, Grid> g_vert = GetClosest2Grids(Center, wall.LevelId ,grids);
                string name = g_vert.Key.Name + "-" + g_vert.Value.Name;

                wall.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set(name);
            }
            foreach (var beam in Beams)
            {
                Curve l = ((LocationCurve)beam.Location).Curve;
                XYZ start= l.GetEndPoint(0);
                XYZ end= l.GetEndPoint(1);
                XYZ Center = (start + end) / 2;
                KeyValuePair<Grid,Grid> g_vert = GetClosest2Grids(Center, beam.LevelId,grids);
                //Grid g_Horz = GetClosestHorizonalGrid(Center, grids);
                string name = g_vert.Key.Name + "-" + g_vert.Value.Name;
                beam.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS).Set(name);

            };
            tr.Commit();
            }
            catch (Exception)
            {
                return Result.Failed;

            }
            return Result.Succeeded;
        }
        //public Grid GetClosestVerticalGrid(XYZ point, List<Grid> grids)
        //{
        //    Grid g = null;
        //    double distance = double.MaxValue;

        //    foreach (Grid grid in grids)
        //    {
        //        XYZ GridStartPoint = grid.Curve.GetEndPoint(0);
        //        XYZ GridEndPoint = grid.Curve.GetEndPoint(1);
        //        XYZ gridvector = ((GridEndPoint - GridStartPoint).Normalize());
        //        double vector = Math.Abs(gridvector.DotProduct(XYZ.BasisY));
        //        if (vector == 1)
        //        {
        //            if (grid.Curve.Distance(point) < distance)
        //            {
        //                distance = grid.Curve.Distance(point);
        //                g = grid;
        //            }
        //        }
        //    }
        //    return g;
        //}
        public KeyValuePair<Grid,Grid> GetClosest2Grids(XYZ point, ElementId l,List<Grid> grids)
        {
            Grid g = null;
            Grid g2 = null;
            double distance = double.MaxValue;
            Dictionary< Grid, double> Lst = new Dictionary<Grid, double>();
            foreach (Grid grid in grids)
            {
                distance = grid.Curve.Distance(point);
                Lst.Add(grid,distance);
            }
            IOrderedEnumerable<KeyValuePair<Grid,double>> OrderedLst = Lst.OrderBy(p => p.Value);
            g = OrderedLst.FirstOrDefault().Key;
            XYZ GStartPoint = g.Curve.GetEndPoint(0);
            XYZ GEndPoint = g.Curve.GetEndPoint(1);
            XYZ gvector = ((GEndPoint - GStartPoint).Normalize());

            foreach (var item in OrderedLst)
            {
                Grid temp = item.Key;
                XYZ GridStartPoint2 = temp.Curve.GetEndPoint(0);
                XYZ GridEndPoint2 = temp.Curve.GetEndPoint(1);
                XYZ gridvector2 = ((GridEndPoint2 - GridStartPoint2).Normalize());

                if (Math.Abs(Math.Abs(gridvector2.DotProduct(gvector))  - 1.0) < 0.001)
                {
                    continue;
                }
                else
                {
                    g2 = temp;
                    break;
                }
            }
            KeyValuePair<Grid, Grid> result = new KeyValuePair<Grid, Grid>(g,g2);
            
            return result;
        }
    }
}
