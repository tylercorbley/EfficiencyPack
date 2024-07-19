using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EfficiencyPack
{
    [Transaction(TransactionMode.Manual)]
    public class CenterCeilingGrid : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument; Document doc = uidoc.Document; List<Element> ceilings = new List<Element>();
            getCeilings(doc, ceilings, uidoc);
            View activeView = doc.ActiveView;
            foreach (Element ceiling in ceilings)
            {
                //outstanding things to do: I need to create a consistent means of dimensioning and a means of creating the offset boundary to the location that i want (currently set to 1). i have a bool that will tell us if the side we are looking at is 4' or not, otherwise we can assume 2' and just take the dimension %2/2 (%4/2 if 4'). need to develop a way to determine which orientation we are facing.
                CeilingRef ceilingRef = new CeilingRef(doc, ceiling as Ceiling);
                string type = ceilingRef.type;
                //from the forum
                //Element elem = doc.GetElement(sel.GetElementIds().FirstOrDefault());
                Element elem = ceiling;
                //Floor floor = elem as Floor;
                //Reference r = HostObjectUtils.GetTopFaces(floor).FirstOrDefault();
                Reference r = ceilingRef.BottomFaceRef;
                //PlanarFace face = elem.GetGeometryObjectFromReference(r) as PlanarFace;
                PlanarFace face = ceilingRef.BottomFace;

                var HatchLines = AnalyzeHatch(elem, r);
                using (TransactionGroup tg = new TransactionGroup(doc, "align2RefPlane"))
                {
                    tg.Start();
                    //sb.AppendLine("elem  " + elem.Id);
                    //sb.AppendLine();
                    int i = 0;
                    foreach (var HLine in HatchLines)//align2RefPlane
                    {
                        int widthOffset = 6;
                        int lengthOffset = (int)Math.Round(ceilingRef.ceilingLength) / 2;
                        if (type.Contains("2x2"))
                        {
                            widthOffset = (int)Math.Round(ceilingRef.ceilingWidth) / 2;
                        }
                        else if (type.Contains("2x4"))
                        {
                            widthOffset = (int)Math.Round(ceilingRef.ceilingWidth) / 4;
                        }
                        List<int> cycles = GetOutermostIndices((int)Math.Round(ceilingRef.ceilingWidth), (int)Math.Round(ceilingRef.ceilingLength));//new List<int> { 8, 17, -4, 19 }; //new 


                        ReferencePlane pl = null;
                        using (Transaction t = new Transaction(doc, "CreateRefPlane"))
                        {
                            t.Start();
                            pl = doc.Create.NewReferencePlane(HLine.Item3.Add(HLine.Item4.Multiply(150)), HLine.Item3, face.FaceNormal.Multiply(3), activeView);//CreateRefPlane
                            pl.Name = string.Format("{0}_{1}", "ref", Guid.NewGuid());
                            t.Commit();
                        }
                        string stableRef = string.Format("{0}:0:{1}", pl.UniqueId, "SURFACE");
                        Reference ref2Plane = Reference.ParseFromStableRepresentation(doc, stableRef);
                        ReferenceArray test1 = HLine.Item5;
                        Reference test2 = test1.get_Item(i);
                        using (Transaction t = new Transaction(doc, "align2RefPlane"))
                        {
                            t.Start();
                            doc.Create.NewAlignment(activeView, pl.GetReference(), HLine.Item2); //align2RefPlane
                            t.Commit();
                        }

                        i++;
                        using (Transaction t = new Transaction(doc, "MovePlane"))
                        {
                            t.Start();
                            XYZ corner = null;
                            var boundary = CreateDetailLinesFromCeiling(doc, ceilingRef, 1);
                            List<Curve> OffsetLines = new List<Curve>();
                            OffsetLines.AddRange(boundary.Item2);
                            foreach (Line cv in OffsetLines)
                            {
                                corner = cv.GetEndPoint(0);
                                break;
                            }
                            XYZ translation = HLine.Item3.Subtract(corner); //MovePlane
                            ElementTransformUtils.MoveElement(doc, pl.Id, translation);
                            t.Commit();
                        }

                        using (Transaction t = new Transaction(doc, "dimension ceiling"))
                        {
                            t.Start();
                            //AddDimensionsToCeiling(doc, ceiling as Ceiling, lengthOffset, widthOffset, ceilingRef, cycles);
                            t.Commit();
                        }
                    }
                    tg.Assimilate();
                }
            }
            return Result.Succeeded;
        }
        public void getCeilings(Document doc, List<Element> ceilings, UIDocument uidoc)
        {
            // Get pre-selected elements
            ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();
            if (selectedIds.Count == 0)
            {
                TaskDialog.Show("Error", "Please select some ceilings before running the tool.");
                return;
            }
            // Filter selected elements to only ceilings
            foreach (ElementId id in selectedIds)
            {
                Element elem = doc.GetElement(id);
                if (elem.Category.Id == new ElementId(BuiltInCategory.OST_Ceilings))
                {
                    ceilings.Add(elem);
                }
            }
            if (ceilings.Count == 0)
            {
                TaskDialog.Show("Error", "No ceilings were selected. Please select some ceilings and try again.");
                return;
            }
        }
        public List<List<ReferencePlane>> GetReferencePlane(Document doc, CeilingRef ceilingRef)//script 1
        {
            List<List<ReferencePlane>> refPlanesCeilings = new List<List<ReferencePlane>>();
            List<ReferenceArray> refList = new List<ReferenceArray>();
            List<ReferencePlane> refPlanesCeiling = new List<ReferencePlane>();
            List<Dimension> dimensions = new List<Dimension>();
            try
            {
                for (int j = 0; j < 4; j = j + 2)
                {
                    int gridCount = ceilingRef.GridCount;
                    string stableRef = ceilingRef.StableRef;
                    PlanarFace bottomFace = ceilingRef.BottomFace;
                    for (int hatchindex = 0; hatchindex < gridCount; hatchindex++)
                    {
                        ReferenceArray refAr = new ReferenceArray();
                        for (int ip = j; ip < j * 2; ip++)
                        {
                            int index = (hatchindex + 1) + (ip * gridCount * 2);
                            string stableHatchString = stableRef + string.Format("/{0}", index);
                            Reference hatchRef = Reference.ParseFromStableRepresentation(doc, stableHatchString);
                            refAr.Append(hatchRef);
                        }
                        refList.Add(refAr);
                    }
                    foreach (ReferenceArray refAr in refList)
                    {
                        Dimension refDim = doc.Create.NewDimension(doc.ActiveView, Line.CreateBound(XYZ.Zero, new XYZ(10, 0, 0)), refAr);
                        ElementTransformUtils.MoveElement(doc, refDim.Id, new XYZ(.1, 0, 0));
                        Reference r1 = refDim.References.get_Item(0);
                        Line line = refDim.Curve as Line;
                        XYZ direction = line.Direction;
                        XYZ hatchDirection = direction.CrossProduct(bottomFace.FaceNormal).Normalize();
                        XYZ origin = refDim.Origin.Subtract(direction.Multiply(refDim.Value.GetValueOrDefault() / 2));
                        DetailCurve detailCurve1 = doc.Create.NewDetailCurve(doc.ActiveView, Line.CreateBound(direction, hatchDirection));
                        DetailCurve detailCurve2 = doc.Create.NewDetailCurve(doc.ActiveView, Line.CreateBound(direction, origin));
                        DetailCurve detailCurve3 = doc.Create.NewDetailCurve(doc.ActiveView, Line.CreateBound(origin, hatchDirection));
                        ReferencePlane refPlane = doc.Create.NewReferencePlane(origin.Add(hatchDirection.Multiply(3)), origin, bottomFace.FaceNormal.Multiply(3), doc.ActiveView);
                        refPlane.Name = string.Format("{0}_{1}", "ref", Guid.NewGuid());
                        stableRef = string.Format("{0}:0:{1}", refPlane.UniqueId, "SURFACE");
                        Reference ref2Plane = Reference.ParseFromStableRepresentation(doc, stableRef);
                        dimensions.Add(refDim);
                        refPlanesCeiling.Add(refPlane);
                    }
                    refPlanesCeilings.Add(refPlanesCeiling);
                }
                return refPlanesCeilings;
            }
            catch (Exception ex)
            {
                // Log the exception
                System.Diagnostics.Debug.WriteLine($"An error occurred: {ex.Message}");
                throw;
            }
            finally
            {
                foreach (Dimension dim in dimensions)
                {
                    // doc.Delete(dim.Id);
                }
            }
        }
        public (List<List<ReferencePlane>>, List<List<int>>) GetPlanesIndex(List<List<ReferencePlane>> refPlanesCeilings)// Script 2 
        {
            List<List<ReferencePlane>> refsPlanesUpAcrossList = new List<List<ReferencePlane>>();
            List<List<int>> refPlanesIndexList = new List<List<int>>();

            List<List<List<ReferencePlane>>> refPlanesAll = new List<List<List<ReferencePlane>>>();
            List<List<List<int>>> refPlanesIndexAll = new List<List<List<int>>>();

            foreach (List<ReferencePlane> refPlanes in refPlanesCeilings)
            {
                List<ReferencePlane> refPlanesUp = new List<ReferencePlane>();
                List<ReferencePlane> refPlanesAcross = new List<ReferencePlane>();
                List<int> refPlanesIndexUp = new List<int>();
                List<int> refPlanesIndexAcross = new List<int>();
                int ind = 0;
                foreach (ReferencePlane refPlane in refPlanes)
                {
                    if (refPlane.Direction.IsAlmostEqualTo(new XYZ(0, 0, 1)) || refPlane.Direction.IsAlmostEqualTo(new XYZ(0, 0, -1)))
                    {
                        refPlanesUp.Add(refPlane);
                        refPlanesIndexUp.Add(ind);
                        ind++;
                    }
                    else if (refPlane.Direction.IsAlmostEqualTo(new XYZ(1, 0, 0)) || refPlane.Direction.IsAlmostEqualTo(new XYZ(-1, 0, 0)))
                    {
                        refPlanesAcross.Add(refPlane);
                        refPlanesIndexAcross.Add(ind);
                        ind++;
                    }

                }
                List<List<ReferencePlane>> refPlanesComb = new List<List<ReferencePlane>> { refPlanesUp, refPlanesAcross };
                List<List<int>> refPlanesIndex = new List<List<int>> { refPlanesIndexUp, refPlanesIndexAcross };

                refPlanesAll.Add(refPlanesComb);
                refPlanesIndexAll.Add(refPlanesIndex);
            }

            for (int i = 0; i < refPlanesAll.Count; i++)
            {
                List<ReferencePlane> refPlanesComb = new List<ReferencePlane>();
                List<int> refPlanesIndex = new List<int>();

                for (int j = 0; j < refPlanesAll[i].Count; j++)
                {
                    if (refPlanesAll[i][j].Any())
                    {
                        refPlanesComb.Add(refPlanesAll[i][j][0]);
                        refPlanesIndex.Add(refPlanesIndexAll[i][j][0]);
                    }
                }
                refsPlanesUpAcrossList.Add(refPlanesComb);
                refPlanesIndexList.Add(refPlanesIndex);
            }
            return (refsPlanesUpAcrossList, refPlanesIndexList);
        }
        public void ProcessCeilingsAndReferencePlanes(Document doc, CeilingRef ceilingRef)//script 3
        {
            var refPlanesCeilings = GetReferencePlane(doc, ceilingRef);   // script 1
            var (refsPlanesUpAcrossList, refPlanesIndexList) = GetPlanesIndex(refPlanesCeilings); //script 2
            List<Dimension> refDimensions = new List<Dimension>();// Start of Script 3 __________________________
            double offset = (Math.Ceiling(ceilingRef.ceilingWidth) % 2) / 2;
            if (offset == 0)
            {
                offset = 2;
            }
            (ReferenceArray refArrayCeilingOffset, List<Curve> curveList) = CreateDetailLinesFromCeiling(doc, ceilingRef, offset);

            for (int i = 0; i < refPlanesIndexList.Count; i++)
            {
                List<ReferencePlane> refPlanes = refsPlanesUpAcrossList[i];
                List<int> indexes = refPlanesIndexList[i];
                for (int j = 0; j < refPlanesIndexList[i].Count; j++)
                {
                    //get the external reference of the wall as an array
                    PlanarFace bottomFace = ceilingRef.BottomFace;
                    Face extFace = ceilingRef.ExteriorFace;
                    int gridCount = ceilingRef.GridCount;
                    IList<Reference> extFaceRefList = ceilingRef.extFaceRef;
                    //foreach (Reference extFaceRef in extFaceRefList)
                    //{
                    //Face extFace = ceilingRef.ceilingObject.GetGeometryObjectFromReference(extFaceRef) as Face;
                    string stableRef = ceilingRef.StableRef;
                    //for the array containing the external face reference, get its geometry
                    XYZ corner = null;
                    foreach (Curve bottomEdgeCurve in extFace.GetEdgesAsCurveLoops()[0])
                    {
                        //get the finish point of the line
                        corner = bottomEdgeCurve.GetEndPoint(1);
                    }

                    //we want horizontal and vertical refs so we want to arrays
                    List<ReferenceArray> refAll = new List<ReferenceArray>();
                    for (int hatchindex = 0; hatchindex < gridCount; hatchindex++)
                    {
                        ReferenceArray refAr = new ReferenceArray();
                        for (int ip = 0; ip < 2; ip++)
                        {
                            //generate an index for each hatch reference
                            int index = (hatchindex + 1) + (ip * gridCount * 2);
                            //create a string for each hatch refeerence using the face reference and hatch index
                            string stableHatchString = stableRef + string.Format("/{0}", index);
                            //generate a new reference fr each hatch reference using the string
                            Reference HatchRef = Reference.ParseFromStableRepresentation(doc, stableHatchString);
                            //the hatch reference is both
                            refAr.Append(HatchRef);
                        }
                        //refList contains arrays, each containing a pair of references
                        //we only want 2 arrays, a horizontal and a vertical
                        refAll.Add(refAr);
                    }
                    //we use the index we returned from the reference plane to
                    //determine the index we need of the references... there aren't
                    //any properties or methods of the reference to determine orientation
                    List<ReferenceArray> refList = indexes.Select(idx => refAll[idx]).ToList();
                    for (int k = 0; k < refList.Count; k++)
                    {
                        ReferenceArray refAr = refList[k];
                        ReferencePlane refPlane = refPlanes[k];
                        Dimension refDim = doc.Create.NewDimension(doc.ActiveView, Line.CreateBound(XYZ.Zero, new XYZ(10, 0, 0)), refAr);
                        ElementTransformUtils.MoveElement(doc, refDim.Id, new XYZ(.1, 0, 0));
                        Reference r1 = refDim.References.get_Item(0);
                        XYZ direction = (refDim.Curve as Line).Direction;
                        XYZ hatchDirection = direction.CrossProduct(bottomFace.FaceNormal).Normalize();
                        XYZ origin = refDim.Origin.Subtract(direction.Multiply(refDim.Value.GetValueOrDefault() / 2));
                        Line testLine = Line.CreateBound(hatchDirection, direction);
                        //DetailCurve detailCurve = doc.Create.NewDetailCurve(doc.ActiveView, Line.CreateBound(origin, new XYZ(10, 0, 0)));
                        stableRef = string.Format("{0}:0:{1}", refPlane.UniqueId, "SURFACE");
                        Reference ref2Plane = Reference.ParseFromStableRepresentation(doc, stableRef);
                        doc.Create.NewAlignment(doc.ActiveView, ref2Plane, r1);
                        XYZ translation = origin.Subtract(curveList[k].GetEndPoint(1));//corner);
                        ElementTransformUtils.MoveElement(doc, refPlane.Id, -translation);
                        refDimensions.Add(refDim);
                        // for each ceiling, we run each refPlane against each reference, 1 hor, 1 vert
                    }
                }
            }
            //}
            // End of Script 3 __________________________
            foreach (Dimension refDim in refDimensions)
            {
                // doc.Delete(refDim.Id);
            }
            foreach (List<ReferencePlane> refPlanes in refPlanesCeilings)
            {
                foreach (ReferencePlane refPlane in refPlanes)
                {
                    doc.Delete(refPlane.Id);
                }
            }
        }//script 3\
        public static Line GetCeilingPerimeter(Element ceiling)
        {
            // Cast ceiling element to Ceiling
            Ceiling ceilingElement = ceiling as Ceiling;
            if (ceilingElement == null)
            {
                // Handle error or return null, depending on your application logic
                return null;
            }

            // Get the bottom face of the ceiling
            Reference bottomFaceRef = HostObjectUtils.GetBottomFaces(ceilingElement).FirstOrDefault();
            if (bottomFaceRef == null)
            {
                // Handle error or return null, depending on your application logic
                return null;
            }

            PlanarFace bottomFace = ceilingElement.GetGeometryObjectFromReference(bottomFaceRef) as PlanarFace;
            if (bottomFace == null)
            {
                // Handle error or return null, depending on your application logic
                return null;
            }

            // Get the edges of the bottom face
            EdgeArrayArray edgeLoops = bottomFace.EdgeLoops;
            List<XYZ> perimeterPoints = new List<XYZ>();

            // Iterate through edge loops to collect perimeter points
            foreach (EdgeArray edgeArray in edgeLoops)
            {
                foreach (Edge edge in edgeArray)
                {
                    Curve edgeCurve = edge.AsCurve();
                    XYZ startPoint = edgeCurve.GetEndPoint(0);
                    XYZ endPoint = edgeCurve.GetEndPoint(1);
                    perimeterPoints.Add(startPoint);
                    //perimeterPoints.Add(endPoint);
                }
            }

            // Create a polyline from collected points
            if (perimeterPoints.Count < 2)
            {
                // Handle case where not enough points were collected
                return null;
            }

            XYZ startPointFinal = perimeterPoints.First();
            XYZ endPointFinal = perimeterPoints.Last();
            Line perimeterLine = Line.CreateBound(startPointFinal, endPointFinal);

            return perimeterLine;
        }
        public void AddDimensionsToCeiling(Document doc, Ceiling ceiling, int lengthOffset, int widthOffset, CeilingRef ceilingRef, List<int> cycles)// this one is from the forum
        {
            List<Reference> edges = GetCeilingEdgeReferences(ceiling);
            edges = ShiftList(edges, 3);
            cycles = ShiftList(cycles, 2);
            //int cycles = Convert.ToInt32(Math.Ceiling(widthOffset / 2)) * 2;
            for (int i = 0; i < 4; i++)
            //for (int i = 2 + cycles; i <= 5 + cycles; i++)
            {
                Reference top = HostObjectUtils.GetBottomFaces(ceiling).First();
                PlanarFace topFace = ceiling.GetGeometryObjectFromReference(top) as PlanarFace;
                //check for model surfacepattern
                Material mat = doc.GetElement(topFace.MaterialElementId) as Material;
                FillPatternElement patterntype = doc.GetElement(mat.SurfaceForegroundPatternId) as FillPatternElement;
                if (patterntype == null) return;
                FillPattern pattern = patterntype.GetFillPattern();
                if (pattern.IsSolidFill || pattern.Target == FillPatternTarget.Drafting) return;

                // get number of gridLines in pattern                
                int _gridCount = pattern.GridCount;

                // construct StableRepresentations and find the Reference to HatchLines
                ReferenceArray _resArr = new ReferenceArray();
                ReferenceArray referenceArray = new ReferenceArray();
                string StableRef = top.ConvertToStableRepresentation(doc);
                for (int j = 1; j < 4 * lengthOffset; j++)
                {
                    int index = j + (0 * _gridCount * 2);
                    string StableHatchString = StableRef + string.Format("/{0}", index);
                    Reference HatchRef = null;
                    try
                    {
                        HatchRef = Reference.ParseFromStableRepresentation(doc, StableHatchString);
                        //CreateDetailLineFromReference(doc, HatchRef);
                    }
                    catch
                    { }
                    if (HatchRef == null) continue;
                    referenceArray.Append(HatchRef);
                }
                int k = GetLastValidIndexBeforeReference(doc, edges[i], referenceArray);//test for parallel to determine comparison direction?
                int index2 = k + (0 * _gridCount * 2);
                string StableHatchString2 = StableRef + string.Format("/{0}", index2);
                Reference HatchRef2 = null;
                try
                {
                    HatchRef2 = Reference.ParseFromStableRepresentation(doc, StableHatchString2);
                }
                catch
                { }
                if (HatchRef2 == null) continue;
                _resArr.Append(HatchRef2);
                _resArr.Append(edges[i]);
                // 2 or more References => create dimension
                if (_resArr.Size > 1)
                {
                    Dimension _dimension = doc.Create.NewDimension(doc.ActiveView, Line.CreateBound(XYZ.Zero, new XYZ(.1, 0, 0)), _resArr);
                    // move dimension a tiny amount to orient the dimension perpendicular to the hatchlines
                    // I can't say why it works, but it does.
                    ElementTransformUtils.MoveElement(doc, _dimension.Id, new XYZ(.1, 0, 0));
                    // Override the dimension text with "EQ"
                    _dimension.ValueOverride = "EQ";
                }

            }
        }
        public List<Reference> GetCeilingEdgeReferences(Ceiling ceiling)
        {
            List<Reference> edgeReferences = new List<Reference>();

            // Get the geometry of the ceiling
            Options geomOptions = new Options();
            geomOptions.ComputeReferences = true;
            GeometryElement geomElem = ceiling.get_Geometry(geomOptions);

            foreach (GeometryObject geomObj in geomElem)
            {
                if (geomObj is Solid)
                {
                    Solid solid = geomObj as Solid;

                    // Find the bottom face
                    PlanarFace bottomFace = solid.Faces.OfType<PlanarFace>()
                        .OrderBy(f => f.Origin.Z)
                        .FirstOrDefault();

                    if (bottomFace != null)
                    {
                        // Get the edges of the bottom face
                        EdgeArrayArray edgeLoops = bottomFace.EdgeLoops;
                        if (edgeLoops.Size > 0)
                        {
                            EdgeArray edges = edgeLoops.get_Item(0);
                            foreach (Edge edge in edges)
                            {
                                if (edge.Reference != null)
                                {
                                    edgeReferences.Add(edge.Reference);
                                }
                            }
                        }
                    }

                    // We only need to process the first solid
                    break;
                }
            }

            return edgeReferences;
        }
        public static List<T> ShiftList<T>(List<T> list, int shiftBy)
        {
            if (list == null || list.Count == 0)
                return list;

            shiftBy = shiftBy % list.Count;
            if (shiftBy < 0)
                shiftBy += list.Count;

            return list.Skip(list.Count - shiftBy).Concat(list.Take(list.Count - shiftBy)).ToList();
        }
        public static List<int> GetOutermostIndices(int gridWidth, int gridHeight)
        {
            int horizontalRings = (gridWidth + 1) / 2 / 2;
            int verticalRings = (gridHeight + 1) / 2 / 2;

            // Calculate the indices for each corner
            int top = 4 * horizontalRings - 2;
            int bottom = top + 2;
            int right = 4 * verticalRings + 1 - 2;
            int left = right + 2;

            return new List<int> { top, right, bottom, left };
        }
        public enum ComparisonType
        {
            GreaterThan,
            LessThan
        }
        public int GetLastValidIndexBeforeReference(Document doc, Reference referenceX, ReferenceArray referenceArray)
        {
            // Get the location and direction of Reference X
            Element elementX = doc.GetElement(referenceX);
            XYZ pointX = GetElementLocationPoint(elementX);
            XYZ directionX = GetElementDirection(elementX);

            if (pointX == null || directionX == null)
            {
                TaskDialog.Show("Error", "Reference X location or direction could not be determined.");
                return -1; // Indicating an error
            }

            int lastValidIndex = -1;
            double maxDistance = 2.0; // 2 feet

            // Iterate through the ReferenceArray
            for (int i = 0; i < referenceArray.Size; i++)
            {
                Reference reference = referenceArray.get_Item(i);
                Element element = doc.GetElement(reference);
                XYZ point = GetElementLocationPoint(element);
                XYZ direction = GetElementDirection(element);

                if (point == null || direction == null)
                {
                    TaskDialog.Show("Error", $"Reference at index {i} location or direction could not be determined.");
                    continue;
                }

                // Check if the direction is parallel to Reference X's direction
                if (IsParallel(directionX, direction))
                {
                    // Check if the point is within 2 feet of Reference X
                    if (IsWithinDistance(pointX, point, maxDistance))
                    {
                        lastValidIndex = i;
                    }
                }
            }

            return lastValidIndex;
        }
        private XYZ GetElementLocationPoint(Element element)
        {
            Location location = element.Location;

            if (location is LocationPoint locPoint)
            {
                return locPoint.Point;
            }
            else if (location is LocationCurve locCurve)
            {
                // For simplicity, return the midpoint of the curve
                Curve curve = locCurve.Curve;
                return curve.Evaluate(0.5, true);
            }

            return null;
        }
        private XYZ GetElementDirection(Element element)
        {
            Location location = element.Location;

            if (location is LocationCurve locCurve)
            {
                Curve curve = locCurve.Curve;
                return curve.GetEndPoint(1) - curve.GetEndPoint(0);
            }

            return null;
        }
        private bool IsParallel(XYZ dir1, XYZ dir2)
        {
            // Normalize the direction vectors
            dir1 = dir1.Normalize();
            dir2 = dir2.Normalize();

            // Check if the cross product is zero
            XYZ crossProduct = dir1.CrossProduct(dir2);
            return crossProduct.IsZeroLength();
        }
        private bool IsWithinDistance(XYZ point1, XYZ point2, double maxDistance)
        {
            // Calculate the distance between the two points
            double distance = point1.DistanceTo(point2);
            return distance <= maxDistance;
        }
        public (ReferenceArray, List<Curve>) CreateDetailLinesFromCeiling(Document doc, CeilingRef ceilingRef, double offsetFeet)
        {
            ReferenceArray referenceArray = new ReferenceArray();
            List<Curve> curveList = new List<Curve>();
            // Get the active view
            Autodesk.Revit.DB.View view = doc.ActiveView;

            // Get the bounding box of the ceiling
            BoundingBoxXYZ boundingBox = ceilingRef.ceilingObject.get_BoundingBox(view);
            if (boundingBox == null)
            {
                TaskDialog.Show("Error", "Bounding box of the ceiling could not be determined.");
                return (null, null);
            }

            // Offset the bounding box inside
            double offset = UnitUtils.ConvertToInternalUnits(offsetFeet, UnitTypeId.Feet); // Offset in feet
            XYZ min = boundingBox.Min;
            XYZ max = boundingBox.Max;

            XYZ offsetMin = new XYZ(min.X + offset, min.Y + offset, min.Z);
            XYZ offsetMax = new XYZ(max.X - offset, max.Y - offset, max.Z);

            // Define the corners of the offset bounding box
            XYZ p1 = new XYZ(offsetMin.X, offsetMin.Y, offsetMin.Z);
            XYZ p2 = new XYZ(offsetMax.X, offsetMin.Y, offsetMin.Z);
            XYZ p3 = new XYZ(offsetMax.X, offsetMax.Y, offsetMin.Z);
            XYZ p4 = new XYZ(offsetMin.X, offsetMax.Y, offsetMin.Z);

            // Create detail lines at the offset locations
            List<XYZ> points = new List<XYZ> { p1, p2, p3, p4, p1 };

            for (int i = 0; i < points.Count - 1; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]);
                curveList.Add(line as Curve);
                DetailCurve detailCurve = doc.Create.NewDetailCurve(view, line);
                referenceArray.Append(detailCurve.GeometryCurve.Reference);
            }

            return (referenceArray, curveList);
        }
        List<Tuple<int, Reference, XYZ, XYZ, ReferenceArray, bool>> AnalyzeHatch(Element elem, Reference hatchface)
        {
            //check for model surfacepattern
            List<Tuple<int, Reference, XYZ, XYZ, ReferenceArray, bool>> res = new List<Tuple<int, Reference, XYZ, XYZ, ReferenceArray, bool>>();
            Document doc = elem.Document;
            View activeView = doc.ActiveView;
            PlanarFace face = elem.GetGeometryObjectFromReference(hatchface) as PlanarFace;
            Material mat = doc.GetElement(face.MaterialElementId) as Material;
            FillPatternElement patterntype = doc.GetElement(mat.SurfaceForegroundPatternId) as FillPatternElement;
            if (patterntype == null) return res;
            FillPattern pattern = patterntype.GetFillPattern();
            if (pattern.IsSolidFill || pattern.Target == FillPatternTarget.Drafting) return res;

            // get number of gridLines in pattern                
            int _gridCount = pattern.GridCount;

            // construct StableRepresentations and find the Reference to HatchLines
            string StableRef = hatchface.ConvertToStableRepresentation(doc);
            using (Transaction t = new Transaction(doc, "analyse hatch"))
            {
                //if (!doc.IsModifiable)
                // {
                t.Start();
                //  }
                for (int hatchindex = 0; hatchindex < _gridCount; hatchindex++)
                {
                    ReferenceArray _resArr = new ReferenceArray();
                    for (int ip = 0; ip < 2; ip++)
                    {
                        int index = (hatchindex + 1) + (ip * _gridCount * 2);
                        string StableHatchString = StableRef + string.Format("/{0}", index);

                        Reference HatchRef = null;
                        try
                        {
                            HatchRef = Reference.ParseFromStableRepresentation(doc, StableHatchString);
                        }
                        catch
                        { }
                        if (HatchRef == null) continue;
                        _resArr.Append(HatchRef);
                    }
                    double height = elem.get_Parameter(BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM).AsDouble();
                    Level level = doc.GetElement(elem.LevelId) as Level;
                    height = height + level.Elevation;
                    // 2 or more References => create dimension
                    if (_resArr.Size > 1)
                    {
                        using (SubTransaction st = new SubTransaction(doc))
                        {
                            st.Start();

                            Dimension _dimension = doc.Create.NewDimension(activeView, Line.CreateBound(new XYZ(0, 0, height), new XYZ(10, 0, height)), _resArr);
                            bool fourFeet = false;
                            if (_dimension.Value == 4)
                            {
                                fourFeet = true;
                            }
                            // move dimension a tiny amount to orient the dimension perpendicular to the hatchlines
                            // I can't say why it works, but it does.
                            ElementTransformUtils.MoveElement(doc, _dimension.Id, new XYZ(.1, 0, 0));

                            Reference r1 = _dimension.References.get_Item(0);
                            XYZ direction = (_dimension.Curve as Line).Direction;
                            XYZ hatchDirection = direction.CrossProduct(face.FaceNormal).Normalize();
                            XYZ origin = _dimension.Origin.Subtract(direction.Multiply((double)_dimension.Value / 2));
                            res.Add(new Tuple<int, Reference, XYZ, XYZ, ReferenceArray, bool>(hatchindex, r1, origin, hatchDirection, _resArr, fourFeet));
                            st.RollBack();

                        }
                    }
                }
            }
            return res;
        }
        public static String GetMethod()
        {
            var method = MethodBase.GetCurrentMethod().DeclaringType?.FullName;
            return method;
        }
    }
    public class CeilingRef
    {
        public HostObject HostObject { get; private set; }
        public Reference BottomFaceRef { get; private set; }
        public IList<Reference> extFaceRef { get; private set; }
        public PlanarFace BottomFace { get; private set; }
        public Face ExteriorFace { get; private set; }
        public Material Material { get; private set; }
        public FillPattern FillPattern { get; private set; }
        public int GridCount { get; private set; }
        public string StableRef { get; private set; }
        public Ceiling ceilingObject { get; private set; }
        public double ceilingWidth { get; private set; }
        public double ceilingLength { get; private set; }
        public string type { get; private set; }
        public CeilingRef(Document doc, Ceiling ceiling)
        {
            InitializeCeilingRef(doc, ceiling);
        }

        private void InitializeCeilingRef(Document doc, Ceiling ceiling)
        {
            HostObject = ceiling as HostObject;
            if (HostObject == null)
                throw new InvalidOperationException("Ceiling is not a valid HostObject.");

            BottomFaceRef = HostObjectUtils.GetBottomFaces(HostObject).FirstOrDefault();
            if (BottomFaceRef == null)
                throw new InvalidOperationException("Failed to retrieve the bottom face of the ceiling.");

            // extFaceRef = HostObjectUtils.GetSideFaces(ceiling, ShellLayerType.Exterior);

            BottomFace = ceiling.GetGeometryObjectFromReference(BottomFaceRef) as PlanarFace;
            if (BottomFace == null)
                throw new InvalidOperationException("Failed to retrieve the bottom planar face.");

            ExteriorFace = ceiling.GetGeometryObjectFromReference(BottomFaceRef) as Face;
            if (ExteriorFace == null)
                throw new InvalidOperationException("Failed to retrieve the exterior face.");

            Material = doc.GetElement(ExteriorFace.MaterialElementId) as Material;
            if (Material == null)
                throw new InvalidOperationException("Failed to retrieve the material of the exterior face.");

            FillPatternElement patternElement = doc.GetElement(Material.SurfaceForegroundPatternId) as FillPatternElement;
            if (patternElement == null)
                throw new InvalidOperationException("Failed to retrieve the fill pattern element.");

            ceilingObject = ceiling as Ceiling;

            // Get the ceiling type name
            ElementId typeId = ceiling.GetTypeId();
            string ceilingType = "";
            Element ceilingTypeElement = doc.GetElement(ceiling.GetTypeId());
            if (ceilingTypeElement != null)
            {
                Parameter typeCommentsParam = ceilingTypeElement.LookupParameter("Type Comments");
                if (typeCommentsParam != null && typeCommentsParam.HasValue)
                {
                    ceilingType = typeCommentsParam.AsString();
                }
            }
            type = ceilingType;
            // Get the geometry of the ceiling
            GeometryElement geomElement = ceiling.get_Geometry(new Options());

            foreach (GeometryObject geomObj in geomElement)
            {
                if (geomObj is Solid solid)
                {
                    // Get the bounding box of the solid
                    BoundingBoxXYZ bbox = solid.GetBoundingBox();

                    // Calculate width and length
                    double width = Math.Abs(bbox.Max.X - bbox.Min.X);
                    double length = Math.Abs(bbox.Max.Y - bbox.Min.Y);
                    ceilingWidth = width;
                    ceilingLength = length;

                    // Convert from feet to meters if needed
                    //info.Width = width;//UnitUtils.ConvertFromInternalUnits(width, UnitTypeId.Meters);
                    //info.Length = length;//UnitUtils.ConvertFromInternalUnits(length, UnitTypeId.Meters);

                    break; // Assuming the first solid is the main geometry
                }
            }
            FillPattern = patternElement.GetFillPattern();
            GridCount = FillPattern.GridCount;
            StableRef = BottomFaceRef.ConvertToStableRepresentation(doc);
        }
    }
}