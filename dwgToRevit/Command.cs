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

#endregion Namespaces

namespace SOM.RevitTools.ChangeProjectUnits
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        // offset the input line by the given distance
        private Line Offset(Line line, double distance)
        {
            XYZ direction = line.Direction;
            XYZ normal = XYZ.BasisZ.CrossProduct(direction).Normalize();
            XYZ translate = normal.Multiply(distance);

            XYZ start = line.GetEndPoint(0).Add(translate);
            XYZ end = line.GetEndPoint(1).Add(translate);

            return Line.CreateBound(start, end);
        }


        // this class will be used to keep track of a line
        // it's distance to the origin axis
        // if it has been checked or
        // which direciton it faces
        private class LineDistance
        { 
            // the original lie
            public Line Line
            {
                get; set;
            }
            // this is the distance to the axis the line is on
            public double Distance
            {
                get; set;
            }
            // This stores if the wall has been checked, or paired with a line
            public bool Checked
            {
                get; set;
            }
            // which direction does the wall face?
            public bool Normal
            {
                get; set;
            }
        }

        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            // get the document
            Document doc = commandData.Application.ActiveUIDocument.Document;            

            // start a transaction
            using (Transaction t = new Transaction(doc, "Change Project Units"))
            {
                t.Start();

                Level level = new FilteredElementCollector(doc).OfClass(typeof(Level)).FirstOrDefault() as Level;

                // get all lines 
                List<Line> lines = new List<Line>();
                FilteredElementCollector collector = new FilteredElementCollector(doc).OfClass(typeof(CurveElement)).OfCategory(BuiltInCategory.OST_Lines);
                foreach (CurveElement l in collector)
                {
                    string style = doc.GetElement(l.LookupParameter("Line Style").AsElementId()).Name;
                    if (style == "A-WALL")
                    {
                        lines.Add(l.GeometryCurve as Line);
                    }
                }

                Dictionary<string, List<LineDistance>> directions = new Dictionary<string, List<LineDistance>>();
                foreach (Line l in lines)
                {
                    XYZ vector = l.Direction;
                    //bool found = false;

                    string absoluteDirection = Math.Abs(vector.X).ToString() + Math.Abs(vector.Y).ToString() + Math.Abs(vector.Z).ToString();

                    LineDistance dist = new LineDistance();
                    dist.Line = l;

                    if (directions.ContainsKey(absoluteDirection))
                    {
                        directions[absoluteDirection].Add(dist);
                    }
                    else
                    {                        
                        directions[absoluteDirection] = new List<LineDistance>() { dist };
                    }


                }

                IList<Line> wallLines = new List<Line>();
                foreach (List<LineDistance> direction in directions.Values)
                {
                    LineDistance baseline = direction[0];
                    Line axis;
                    XYZ axisPoint;
                    XYZ negativeAxisPoint;
                    if (baseline.Line.Direction.X == 0 && baseline.Line.Direction.Y == 1 )
                    {
                        axis = Line.CreateUnbound(new XYZ(0, 0, 0), new XYZ(1, 0, 0));
                        axisPoint = new XYZ(1, 0, 1);
                        negativeAxisPoint = new XYZ(-1, 0, -1);
                    } else
                    {
                        axis = Line.CreateUnbound(new XYZ(0, 0, 0), new XYZ(0, 1, 0));
                        axisPoint = new XYZ(0, 1, 1);
                        negativeAxisPoint = new XYZ(0, -1, -1);
                    }

                    foreach (LineDistance line in direction)
                    {
                        IntersectionResultArray intersect1 = new IntersectionResultArray();
                        Line temp = line.Line.Clone() as Line;
                        temp.MakeUnbound();
                        temp.Intersect(axis, out intersect1);
                        XYZ point1 = intersect1.get_Item(0).XYZPoint;
                        line.Distance = XYZ.Zero.DistanceTo(point1);
                    }

                    

                    direction.OrderBy(s => s.Line.GetEndPoint(0).X).ThenBy(s => s.Line.GetEndPoint(0).Y);

                    for (int i=0; i < direction.Count-1; i ++)
                    {
                        if (direction[i].Checked) {
                            continue;
                        }
                        bool added = false;
                        List<LineDistance> applicable = new List<LineDistance>();
                        for (int j=i+1;  j < direction.Count-1; j++)
                        {
                            double diff = Math.Abs(direction[i].Distance - direction[j].Distance);
                            if (diff > 0 && diff < 1.25)
                            {
                                // check how close they are to each other
                                Line longerLine;
                                XYZ projectPoint1;
                                XYZ projectPoint2;
                                if (direction[i].Line.Length > direction[j].Line.Length)
                                {
                                    longerLine = direction[i].Line.Clone() as Line;
                                    projectPoint1 = direction[j].Line.GetEndPoint(0);
                                    projectPoint2 = direction[j].Line.GetEndPoint(1);
                                } else
                                {
                                    longerLine = direction[j].Line.Clone() as Line;
                                    projectPoint1 = direction[i].Line.GetEndPoint(0);
                                    projectPoint2 = direction[i].Line.GetEndPoint(1);
                                }

                                longerLine.MakeUnbound();
                                IntersectionResult projection1 = longerLine.Project(projectPoint1);
                                IntersectionResult projection2 = longerLine.Project(projectPoint2);

                                if (Math.Abs(projection1.Distance) < 1.25 || Math.Abs(projection2.Distance) < 1.25) {

                                    applicable.Add(direction[j]);
                                    direction[j].Checked = true;
                                    if (!added)
                                    {
                                        applicable.Add(direction[i]);
                                        added = true;
                                    }
                                }

                            } else if (diff > 1.25)
                            {
                                break;
                            }


                            
                        }

                        // add the longest line to the list of applicable lines
                        double longest = 0;
                        int longestIndex = 0;
                        bool lineDirection = true;

                        for (int p = 0; p < applicable.Count; p++)
                        {
                            if (applicable[p].Line.Length > longest)
                            {
                                if (applicable[p].Distance > applicable[longestIndex].Distance)
                                {
                                    lineDirection = true;
                                } else
                                {
                                    lineDirection = false;
                                }
                                longest = applicable[p].Line.Length;
                                longestIndex = p;
                            }
                        }

                        if (longest > 0)
                        {
                            applicable[longestIndex].Normal = lineDirection;
                            Line shifted = Offset(applicable[longestIndex].Line, 0.25);

                            wallLines.Add(shifted);
                            
                        }
                    }
                    
                }
                List<Wall> walls = new List<Wall>();
                foreach (Line c in wallLines)
                {
                    try
                    {
                        walls.Add(Wall.Create(doc, c, level.Id, false));
                    }
                    catch
                    {
                        // do nothing
                    }
                }

                // next create doors
                List<FamilyInstance> doors = new List<FamilyInstance>();

                FilteredElementCollector doorCollector = new FilteredElementCollector(doc).OfClass(typeof(FamilySymbol)).OfCategory(BuiltInCategory.OST_Doors);
                FamilySymbol doorType = doorCollector.FirstOrDefault() as FamilySymbol;
                Dictionary<string, List<XYZ>> doorMap = new Dictionary<string, List<XYZ>>();
                List<XYZ> doorLocations = new List<XYZ>();

                FilteredElementCollector dwgCollector = new FilteredElementCollector(doc).OfClass(typeof(ImportInstance));
                foreach (ImportInstance import in dwgCollector)
                {
                    if (import.LookupParameter("Name").AsString().ToLower().Contains("door")) {
                        XYZ location = import.GetTransform().Origin;
                        doorLocations.Add(location);
                        // check which wall this door is closest to
                        foreach (Wall wall in walls)
                        {
                            try
                            {
                                LocationCurve wallLocation = wall.Location as LocationCurve;
                                // this assumes all walls are only about 6" thick
                                if (wallLocation.Curve.Distance(location) < 0.5)
                                {
                                    if (doorMap.ContainsKey(wall.Id.ToString()))
                                    {
                                        doorMap[wall.Id.ToString()] = new List<XYZ>() { location };
                                    } else
                                    {
                                        doorMap[wall.Id.ToString()].Add(location);
                                    }
                                }
                            } catch
                            {
                                // do nothing
                            }
                        }
                    }
                }

                // create all the walls using the default type
                foreach (string id in doorMap.Keys)
                {
                    Wall wall = doc.GetElement(id) as Wall;
                    foreach (XYZ door in doorMap[id])
                    {
                        doc.Create.NewFamilyInstance(door, doorType, wall, Autodesk.Revit.DB.Structure.StructuralType.NonStructural);
                    }
                }

                t.Commit();
            }
            return Result.Succeeded;       

        }        
    }
}