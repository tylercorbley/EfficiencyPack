using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Line = Autodesk.Revit.DB.Line;
using Reference = Autodesk.Revit.DB.Reference;
using String = System.String;
using View = Autodesk.Revit.DB.View;

namespace EfficiencyPack
{
    [Transaction(TransactionMode.Manual)]
    public class CenterCeilingGrid : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument; Autodesk.Revit.DB.Document doc = uidoc.Document; List<Element> ceilings = new List<Element>();
            getCeilings(doc, ceilings, uidoc);
            View activeView = doc.ActiveView;
            FrmCenterCeilingGrid formCenterCeilingGrid = new FrmCenterCeilingGrid();
            formCenterCeilingGrid.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            if (formCenterCeilingGrid.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                using (TransactionGroup tg = new TransactionGroup(doc, "align2RefPlane"))
                {
                    bool Horizontal = formCenterCeilingGrid.IsHorizontalSelected;
                    bool Vertical = formCenterCeilingGrid.IsVerticalSelected;
                    double rotationAngle = 0;
                    Line rotationAxis = null;
                    tg.Start();
                    foreach (Element ceiling in ceilings)
                    {

                        (rotationAxis, rotationAngle) = RotateCeilingAroundCenterPoint(doc, ceiling as Ceiling);
                        XYZ centerPoint = GetCeilingCenterPoint(ceiling as Ceiling);
                        CeilingRef ceilingRef = new CeilingRef(doc, ceiling as Ceiling);
                        string type = ceilingRef.type;
                        Element elem = ceiling;
                        Reference r = ceilingRef.BottomFaceRef;
                        PlanarFace face = ceilingRef.BottomFace;
                        double offset = 0;
                        List<double> offsets = new List<double>();
                        var HatchLines = AnalyzeHatch(elem, r);
                        foreach (var HLine in HatchLines)//align2RefPlane
                        {
                            bool useOffsetCenter = (HLine.Item7 && Horizontal) || (!HLine.Item7 && Vertical);

                            offset = GridOffset(HLine.Item6, HLine.Item7, ceilingRef, useOffsetCenter);
                            // TileOffset(HLine.Item6, HLine.Item7, ceilingRef);
                            offsets.Add(offset);

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
                            using (Transaction t = new Transaction(doc, "align2RefPlane"))
                            {
                                t.Start();
                                doc.Create.NewAlignment(activeView, pl.GetReference(), HLine.Item2); //align2RefPlane
                                t.Commit();
                            }

                            using (Transaction t = new Transaction(doc, "align to edge"))
                            {
                                t.Start();
                                XYZ corner = null;
                                List<Curve> boundary = CreateDetailLinesFromCeiling(doc, ceilingRef, offset);
                                foreach (Line cv in boundary)
                                {
                                    corner = cv.GetEndPoint(0);
                                    break;
                                }
                                ElementTransformUtils.MoveElement(doc, pl.Id, corner - pl.FreeEnd);
                                doc.Delete(pl.Id);
                                t.Commit();
                            }
                        }
                        using (Transaction t = new Transaction(doc, "dimension ceiling"))
                        {
                            t.Start();
                            if (formCenterCeilingGrid.DimensionTiles)
                            {
                                AddDimensionsToCeiling(doc, ceiling as Ceiling, ceilingRef, offsets);
                            }
                            if (!AreApproximatelyEqual(Math.Abs(rotationAngle) % (90 * Math.PI / 180), 0) && !AreApproximatelyEqual(Math.Abs(rotationAngle) % (90 * Math.PI / 180), (90 * Math.PI / 180)))
                            {
                                ElementTransformUtils.RotateElement(doc, ceiling.Id, rotationAxis, -rotationAngle);
                            }
                            t.Commit();
                        }
                    }
                    tg.Assimilate();
                }
            }
            return Result.Succeeded;
        }
        public (Line, double) RotateCeilingAroundCenterPoint(Autodesk.Revit.DB.Document doc, Ceiling ceiling)
        {
            Reference BottomFaceRef = HostObjectUtils.GetBottomFaces(ceiling as HostObject).FirstOrDefault();
            // Get the bottom face of the ceiling
            Face bottomFace = ceiling.GetGeometryObjectFromReference(BottomFaceRef) as Face;
            EdgeArrayArray EAA = bottomFace.EdgeLoops;
            EdgeArray EA = EAA.get_Item(0);
            Edge E = EA.get_Item(0);
            Curve curve = E.AsCurve();
            Line line = curve as Line;

            // Compute the center point of the ceiling
            XYZ centerPoint = GetFaceCenter(bottomFace);
            // Create a point 10 units along the X-axis from the center point
            XYZ pointXPlus10 = new XYZ(centerPoint.X + 10, centerPoint.Y, centerPoint.Z);
            ReferencePlane edgePlane = null;
            ReferencePlane referencePlane = null;
            using (Transaction t = new Transaction(doc, "rotate ceiling"))
            {
                t.Start();
                edgePlane = doc.Create.NewReferencePlane(line.GetEndPoint(0), line.GetEndPoint(1), XYZ.BasisZ, doc.ActiveView);
                referencePlane = doc.Create.NewReferencePlane(centerPoint, pointXPlus10, XYZ.BasisZ, doc.ActiveView);
                t.Commit();
            }
            // Calculate the rotation angle to align the ceiling parallel with the reference plane
            Line rotationAxis = Line.CreateBound(centerPoint, new XYZ(centerPoint.X, centerPoint.Y, centerPoint.Z + 10));
            double rotationAngle = CalculateRotationAngle(edgePlane, referencePlane);
            // Rotate the ceiling around the center point
            if (!AreApproximatelyEqual(Math.Abs(rotationAngle) % (90 * Math.PI / 180), 0) && !AreApproximatelyEqual(Math.Abs(rotationAngle) % (90 * Math.PI / 180), (90 * Math.PI / 180)))
            {
                RecreateCeilingSketchLines(doc, ceiling);
                using (Transaction t = new Transaction(doc, "rotate ceiling"))
                {
                    t.Start();
                    ElementTransformUtils.RotateElement(doc, ceiling.Id, rotationAxis, rotationAngle);
                    t.Commit();
                }
            }
            using (Transaction t = new Transaction(doc, "rotate ceiling"))
            {
                t.Start();
                doc.Delete(referencePlane.Id);
                doc.Delete(edgePlane.Id);
                t.Commit();
            }
            return (rotationAxis, rotationAngle);
        }
        public void RecreateCeilingSketchLines(Document doc, Ceiling ceiling)
        {
            if (ceiling == null)
            {
                TaskDialog.Show("Error", "No ceiling provided.");
                return;
            }

            Sketch sketch = doc.GetElement(ceiling.SketchId) as Sketch;
            if (sketch == null)
            {
                TaskDialog.Show("Error", "Ceiling does not have a valid sketch.");
                return;
            }

            // Start a sketch edit scope
            SketchEditScope sketchEditScope = new SketchEditScope(doc, "Recreate ceiling sketch lines");
            sketchEditScope.Start(sketch.Id);

            using (Transaction transaction = new Transaction(doc, "Recreate ceiling sketch lines"))
            {
                transaction.Start();

                try
                {
                    foreach (CurveArray curveArray in sketch.Profile)
                    {
                        foreach (Curve curve in curveArray)
                        {
                            // Store the original curve geometry
                            Curve originalCurve = curve.Clone();

                            // Remove the old curve
                            doc.Delete(curve.Reference.ElementId);

                            // Create a new model curve in the same location
                            doc.Create.NewModelCurve(originalCurve, sketch.SketchPlane);
                        }
                    }

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.RollBack();
                    TaskDialog.Show("Error", "Failed to recreate ceiling sketch lines: " + ex.Message);
                }
            }

            sketchEditScope.Commit(new RoomWarningSwallower());
        }
        private XYZ GetFaceCenter(Face face)
        {
            BoundingBoxUV bb = face.GetBoundingBox();
            UV centerUV = (bb.Min + bb.Max) / 2;
            return face.Evaluate(centerUV);
        }
        private double CalculateRotationAngle(ReferencePlane edgePlane, ReferencePlane referencePlane)
        {
            // Assuming the face normal is pointing upward, we need to align it with the reference plane normal
            XYZ refPlaneNormal = referencePlane.GetPlane().Normal;
            XYZ edgePlaneNormal = edgePlane.GetPlane().Normal;

            return edgePlaneNormal.AngleTo(refPlaneNormal);
        }
        public double TileOffset(bool Item6, bool Item7, CeilingRef ceilingRef)
        {
            double offset = 0;
            if (!Item7)
            {
                if (Item6)
                {
                    offset = (ceilingRef.ceilingLength % 4) / 2;
                }
                else
                {
                    offset = (ceilingRef.ceilingLength % 2) / 2;
                    if (AreApproximatelyEqual(offset, 0))
                    {
                        offset = 2;
                    }
                    if (AreApproximatelyEqual(offset, 1))
                    {
                        offset = 2;
                    }
                }
            }
            else
            {
                if (Item6)
                {
                    offset = (ceilingRef.ceilingWidth % 4) / 2;
                }
                else
                {
                    offset = (ceilingRef.ceilingWidth % 2) / 2;
                    if (AreApproximatelyEqual(offset, 0))
                    {
                        offset = 2;
                    }
                    if (AreApproximatelyEqual(offset, 1))
                    {
                        offset = 2;
                    }
                }
            }
            if (offset < .25)
            {
                if (AreApproximatelyEqual(offset, 0))
                {
                    return offset;
                }
                if (Item6)
                {
                    offset = 2 + offset;
                }
                else
                {
                    offset = 1 + offset;
                }
            }
            return offset;
        }
        public double GridOffset(bool Item6, bool Item7, CeilingRef ceilingRef, bool Tile)
        {
            double offset = 0;
            if (!Item7)
            {
                if (Item6)
                {
                    offset = (ceilingRef.ceilingLength / 2) % 4;
                    if (!Tile)
                    {
                        offset = (offset + 2) % 4;
                    }
                }
                else
                {
                    offset = (ceilingRef.ceilingLength / 2) % 2;
                    if (!Tile)
                    {
                        offset = (offset + 1) % 2;
                        if (AreApproximatelyEqual(offset, 0))
                        {
                            offset = 2;
                        }
                    }
                }
            }
            else
            {
                if (Item6)
                {
                    offset = (ceilingRef.ceilingWidth / 2) % 4;
                    if (!Tile)
                    {
                        offset = (offset + 2) % 4;
                    }
                }
                else
                {
                    offset = (ceilingRef.ceilingWidth / 2) % 2;
                    if (!Tile)
                    {
                        offset = (offset + 1) % 2;
                        if (AreApproximatelyEqual(offset, 0))
                        {
                            offset = 2;
                        }
                    }
                }
            }
            return offset;
        }
        public void getCeilings(Autodesk.Revit.DB.Document doc, List<Element> ceilings, UIDocument uidoc)
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
        public void AddDimensionsToCeiling(Autodesk.Revit.DB.Document doc, Ceiling ceiling, CeilingRef ceilingRef, List<double> offsets)
        {
            List<Element> HostedElements = GetHostedElements(doc, ceiling as Element);
            TranslateSelectedElements(doc, HostedElements, ceilingRef.ceilingLength);
            List<Reference> edges = GetCeilingBoundingEdgeReferences(ceiling);
            int shifter = 1;
            ShiftList(edges, shifter);
            offsets.Add(offsets[0]);
            offsets.Add(offsets[1]);
            List<double> offsetList = ShiftList(offsets, shifter);
            for (int i = 0; i < 4; i++)
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
                string StableRef = top.ConvertToStableRepresentation(doc);
                List<Curve> boundary = CreateDetailLinesFromCeiling(doc, ceilingRef, offsets[i]);
                List<Curve> lines = new List<Curve>();
                lines.AddRange(boundary);
                Line line = lines[i] as Line;
                Reference HatchRef = null;
                int j = 0;
                double height = DimHeight(doc, ceiling);
                while (j < 100)
                {
                    ReferenceArray _resArr = new ReferenceArray();
                    int index = j + (1 * _gridCount * 2);
                    string StableHatchString = StableRef + string.Format("/{0}", j);
                    HatchRef = Reference.ParseFromStableRepresentation(doc, StableHatchString);
                    _resArr.Append(HatchRef);
                    _resArr.Append(edges[i]);
                    if (_resArr.Size > 1)
                    {
                        Dimension _dimension = doc.Create.NewDimension(doc.ActiveView, Line.CreateBound(new XYZ(0, 0, height), new XYZ(10, 0, height)), _resArr);//create dimension
                        ElementTransformUtils.MoveElement(doc, _dimension.Id, new XYZ(.1, 0, 0));
                        if (!AreApproximatelyEqual(_dimension.Value ?? 0.0, offsetList[i]))
                        {
                            if (AreApproximatelyEqual(_dimension.Value ?? 0.0, offsets[i]))
                            {
                                _dimension.ValueOverride = "EQ";// Override the dimension text with "EQ"
                                ElementTransformUtils.MoveElement(doc, _dimension.Id, GetCeilingCenterPoint(ceiling) - _dimension.Origin);
                                break;
                            }
                            doc.Delete(_dimension.Id);
                        }
                        else
                        {
                            _dimension.ValueOverride = "EQ";// Override the dimension text with "EQ"
                            ElementTransformUtils.MoveElement(doc, _dimension.Id, GetCeilingCenterPoint(ceiling) - _dimension.Origin);
                            break;
                        }
                    }
                    j++;
                }
            }
            TranslateSelectedElements(doc, HostedElements, -ceilingRef.ceilingLength);
        }
        public List<Element> GetHostedElements(Autodesk.Revit.DB.Document doc, Element hostElement)
        {
            List<Element> hostedElements = new List<Element>();

            if (hostElement == null)
                return hostedElements;

            // Get all family instances in the document
            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilyInstance));

            foreach (FamilyInstance famInstance in collector)
            {
                // Check if the family instance is hosted
                Element host = famInstance.Host;
                if (host != null && host.Id == hostElement.Id)
                {
                    hostedElements.Add(famInstance);
                }
            }

            return hostedElements;
        }
        public void TranslateSelectedElements(Autodesk.Revit.DB.Document document, List<Element> selectedElements, double distanceZ)
        {

            if (selectedElements.Count == 0)
            {
                //TaskDialog.Show("Translate Elements", "No elements selected.");
                return;
            }

            XYZ translationVector = new XYZ(0, distanceZ, 0);

            foreach (Element element in selectedElements)
            {
                ElementTransformUtils.MoveElement(document, element.Id, translationVector);
            }
        }
        public List<Reference> GetCeilingBoundingEdgeReferences(Ceiling ceiling)
        {
            List<Reference> boundingEdgeReferences = new List<Reference>();
            Options geomOptions = new Options();
            geomOptions.ComputeReferences = true;
            GeometryElement geomElem = ceiling.get_Geometry(geomOptions);

            foreach (GeometryObject geomObj in geomElem)
            {
                if (geomObj is Solid solid)
                {
                    PlanarFace bottomFace = solid.Faces.OfType<PlanarFace>()
                        .OrderBy(f => f.Origin.Z)
                        .FirstOrDefault();

                    if (bottomFace != null)
                    {
                        EdgeArrayArray edgeLoops = bottomFace.EdgeLoops;
                        if (edgeLoops.Size > 0)
                        {
                            EdgeArray edges = edgeLoops.get_Item(0);

                            // Find the extreme points
                            XYZ minPoint = new XYZ(double.MaxValue, double.MaxValue, double.MaxValue);
                            XYZ maxPoint = new XYZ(double.MinValue, double.MinValue, double.MinValue);

                            foreach (Edge edge in edges)
                            {
                                XYZ start = edge.AsCurve().GetEndPoint(0);
                                XYZ end = edge.AsCurve().GetEndPoint(1);

                                minPoint = new XYZ(Math.Min(minPoint.X, Math.Min(start.X, end.X)),
                                                   Math.Min(minPoint.Y, Math.Min(start.Y, end.Y)),
                                                   Math.Min(minPoint.Z, Math.Min(start.Z, end.Z)));

                                maxPoint = new XYZ(Math.Max(maxPoint.X, Math.Max(start.X, end.X)),
                                                   Math.Max(maxPoint.Y, Math.Max(start.Y, end.Y)),
                                                   Math.Max(maxPoint.Z, Math.Max(start.Z, end.Z)));
                            }

                            // Find and add the four bounding edges
                            foreach (Edge edge in edges)
                            {
                                XYZ start = edge.AsCurve().GetEndPoint(0);
                                XYZ end = edge.AsCurve().GetEndPoint(1);

                                if ((AreApproximatelyEqual(start.X, minPoint.X) && AreApproximatelyEqual(end.X, minPoint.X)) ||
     (AreApproximatelyEqual(start.X, maxPoint.X) && AreApproximatelyEqual(end.X, maxPoint.X)) ||
     (AreApproximatelyEqual(start.Y, minPoint.Y) && AreApproximatelyEqual(end.Y, minPoint.Y)) ||
     (AreApproximatelyEqual(start.Y, maxPoint.Y) && AreApproximatelyEqual(end.Y, maxPoint.Y)))
                                {
                                    if (edge.Reference != null)
                                    {
                                        boundingEdgeReferences.Add(edge.Reference);
                                    }
                                }
                            }
                        }
                    }
                    // We only need to process the first solid
                    break;
                }
            }
            return boundingEdgeReferences;
        }
        public static bool AreApproximatelyEqual(double a, double b)
        {
            double epsilon = 1e-9;
            return Math.Abs(a - b) < epsilon;
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
        public List<Curve> OffsetCeilingSketchLines(Autodesk.Revit.DB.Document doc, CeilingRef ceilingRef, double offset)
        {
            List<Curve> offsetCurves = new List<Curve>();

            // Get the edge loops of the bottom face of the ceiling
            EdgeArrayArray edgeArrayArray = ceilingRef.BottomFace.EdgeLoops;

            foreach (EdgeArray edgeArray in edgeArrayArray)
            {
                CurveLoop curveLoop = new CurveLoop();

                // Convert edge array to curve loop
                foreach (Edge edge in edgeArray)
                {
                    Curve curve = edge.AsCurve();
                    curveLoop.Append(curve);
                }

                // Offset the curve loop
                CurveLoop offsetCurveLoop = CurveLoop.CreateViaOffset(curveLoop, offset, XYZ.BasisZ);

                // Add the offset curves to the list
                foreach (Curve curve in offsetCurveLoop)
                {
                    // doc.Create.NewDetailCurve(doc.ActiveView, curve);
                    offsetCurves.Add(curve);
                }
            }

            return offsetCurves;
        }
        public List<Curve> CreateDetailLinesFromCeiling(Autodesk.Revit.DB.Document doc, CeilingRef ceilingRef, double offset2)
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
                return (null);
            }

            // Offset the bounding box inside
            double offset = UnitUtils.ConvertToInternalUnits(offset2, UnitTypeId.Feet); // Offset in feet
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
                doc.Delete(detailCurve.Id);
            }

            return (curveList);
        }
        List<Tuple<int, Reference, XYZ, XYZ, ReferenceArray, bool, bool>> AnalyzeHatch(Element elem, Reference hatchface)
        {
            int ceilingHatchMin = 0;
            int ceilingHatchMax = 2;
            if (elem.Name.Contains("NRC High") || elem.Name.Contains("NRC Medium"))
            {
                ceilingHatchMin = 1; ceilingHatchMax = 3;
            }
            else if (elem.Name.Contains("Clean Room") || elem.Name.Contains("WOOD_LAY IN"))
            {
                ceilingHatchMin = 2; ceilingHatchMax = 4;
            }
            //check for model surfacepattern
            List<Tuple<int, Reference, XYZ, XYZ, ReferenceArray, bool, bool>> res = new List<Tuple<int, Reference, XYZ, XYZ, ReferenceArray, bool, bool>>();
            Autodesk.Revit.DB.Document doc = elem.Document;
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
                t.Start();
                for (int hatchindex = ceilingHatchMin; hatchindex < ceilingHatchMax; hatchindex++)
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
                    double height = DimHeight(doc, elem as Ceiling);
                    if (_resArr.Size > 1)
                    {
                        using (SubTransaction st = new SubTransaction(doc))
                        {
                            st.Start();

                            Dimension _dimension = doc.Create.NewDimension(activeView, Line.CreateBound(new XYZ(0, 0, height), new XYZ(10, 0, height)), _resArr);
                            bool dir = false;
                            ElementTransformUtils.MoveElement(doc, _dimension.Id, new XYZ(.1, 0, 0));
                            Reference r1 = _dimension.References.get_Item(0);
                            XYZ direction = (_dimension.Curve as Line).Direction;
                            XYZ hatchDirection = direction.CrossProduct(face.FaceNormal).Normalize();
                            if (Math.Abs(hatchDirection.X) < 0.5)
                            {
                                dir = true;
                            }
                            bool fourFeet = false;
                            if (elem.Name.Contains("2x4") || elem.Name.Contains("LAY IN"))
                            {
                                if (AreApproximatelyEqual(_dimension.Value ?? 0.0, 4))
                                {
                                    fourFeet = true;
                                }
                            }
                            XYZ origin = _dimension.Origin.Subtract(direction.Multiply((double)_dimension.Value / 2));
                            res.Add(new Tuple<int, Reference, XYZ, XYZ, ReferenceArray, bool, bool>(hatchindex, r1, origin, hatchDirection, _resArr, fourFeet, dir));
                            st.Commit();
                        }
                    }
                }
            }
            return res;
        }
        public double DimHeight(Autodesk.Revit.DB.Document doc, Ceiling elem)
        {
            double height = elem.get_Parameter(BuiltInParameter.CEILING_HEIGHTABOVELEVEL_PARAM).AsDouble();
            Level level = doc.GetElement(elem.LevelId) as Level;
            return height + level.Elevation;
        }
        public XYZ GetCeilingCenterPoint(Ceiling ceiling)
        {
            if (ceiling == null)
            {
                throw new ArgumentNullException(nameof(ceiling), "Ceiling cannot be null.");
            }

            // Get the ceiling's bounding box in model coordinates
            BoundingBoxXYZ boundingBox = ceiling.get_BoundingBox(null);

            if (boundingBox == null)
            {
                throw new InvalidOperationException("Unable to retrieve ceiling bounding box.");
            }

            // Calculate the center point
            XYZ centerPoint = (boundingBox.Min + boundingBox.Max) * 0.5;

            return centerPoint;
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
        public XYZ centerPoint { get; private set; }
        public bool dir { get; private set; }
        public CeilingRef(Autodesk.Revit.DB.Document doc, Ceiling ceiling)
        {
            InitializeCeilingRef(doc, ceiling);
        }
        private void InitializeCeilingRef(Autodesk.Revit.DB.Document doc, Ceiling ceiling)
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
            CurveArray curveArray = new CurveArray();
            EdgeArrayArray edgeArrayArray = BottomFace.EdgeLoops;
            EdgeArray edgeArray = edgeArrayArray.get_Item(0);

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
                    centerPoint = (bbox.Min + bbox.Max) / 2;
                    foreach (Edge edge in solid.Edges)
                    {
                        curveArray.Append(edge.AsCurve());
                    }
                    Curve firstCurve = curveArray.get_Item(0);
                    dir = CenterCeilingGrid.AreApproximatelyEqual(firstCurve.Length, ceilingWidth);
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
    public class RoomWarningSwallower : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor failuresAccessor)
        {
            IList<FailureMessageAccessor> failList = new List<FailureMessageAccessor>();
            // Inside event handler, get all warnings
            failList = failuresAccessor.GetFailureMessages();
            foreach (FailureMessageAccessor failure in failList)
            {
                // check FailureDefinitionIds against ones that you want to dismiss, 
                FailureDefinitionId failID = failure.GetFailureDefinitionId();
                // prevent Revit from showing Unenclosed room warnings
                if (failID == BuiltInFailures.SketchFailures.InvalidSketch)
                {
                    failuresAccessor.DeleteWarning(failure);
                }
            }

            return FailureProcessingResult.Continue;
        }
    }
}