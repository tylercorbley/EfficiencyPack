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
            using (Transaction mainTransaction = new Transaction(doc, "Center Ceiling Grid"))
            {
                mainTransaction.Start();
                foreach (Element ceiling in ceilings)
                {
                    CeilingRef ceilingRef = new CeilingRef(doc, ceiling as Ceiling);
                    ProcessCeilingsAndReferencePlanes(doc, ceilingRef); //script 3
                    List<Dimension> testDim = null; //replace with create dimension code
                    EfficiencyPack.DimensionText.overrideDim(testDim); //change dimension to EQ
                }
                mainTransaction.Commit();
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
        public List<List<ReferencePlane>> GetReferencePlane(Document doc, CeilingRef ceilingRef)
        {
            List<List<ReferencePlane>> refPlanesCeilings = new List<List<ReferencePlane>>();
            List<ReferenceArray> refList = new List<ReferenceArray>();
            List<ReferencePlane> refPlanesCeiling = new List<ReferencePlane>();
            List<Dimension> dimensions = new List<Dimension>();
            try
            {
                int gridCount = ceilingRef.GridCount;
                string stableRef = ceilingRef.StableRef;
                PlanarFace bottomFace = ceilingRef.BottomFace;
                for (int hatchindex = 0; hatchindex < gridCount; hatchindex++)
                {
                    ReferenceArray refAr = new ReferenceArray();
                    for (int ip = 0; ip < 2; ip++)
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
                    ReferencePlane refPlane = doc.Create.NewReferencePlane(origin.Add(hatchDirection.Multiply(3)), origin, bottomFace.FaceNormal.Multiply(3), doc.ActiveView);
                    refPlane.Name = string.Format("{0}_{1}", "ref", Guid.NewGuid());
                    stableRef = string.Format("{0}:0:{1}", refPlane.UniqueId, "SURFACE");
                    Reference ref2Plane = Reference.ParseFromStableRepresentation(doc, stableRef);
                    dimensions.Add(refDim);
                    refPlanesCeiling.Add(refPlane);
                }
                refPlanesCeilings.Add(refPlanesCeiling);
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
                    doc.Delete(dim.Id);
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
        public void ProcessCeilingsAndReferencePlanes(Document doc, CeilingRef ceilingRef)
        {
            var refPlanesCeilings = GetReferencePlane(doc, ceilingRef);   // script 1
            var (refsPlanesUpAcrossList, refPlanesIndexList) = GetPlanesIndex(refPlanesCeilings); //script 2
            // Start of Script 3 __________________________
            List<Dimension> refDimensions = new List<Dimension>();
            double width = ceilingRef.ceilingWidth;
            double length = ceilingRef.ceilingLength;
            string type = ceilingRef.type;
            length = (length % 2) / 2;
            if (type.Contains("2x2"))
            {
                width = (width % 2) / 2;
            }
            else if (type.Contains("2x4"))
            {
                width = (width % 4) / 2;
            }
            else return;//quit if ceiling doesn't have a 2x4 or 2x2 grid to work off of

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
                        stableRef = string.Format("{0}:0:{1}", refPlane.UniqueId, "SURFACE");
                        Reference ref2Plane = Reference.ParseFromStableRepresentation(doc, stableRef);
                        doc.Create.NewAlignment(doc.ActiveView, ref2Plane, r1);
                        XYZ translation = origin.Subtract(corner);
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
                doc.Delete(refDim.Id);
            }
            foreach (List<ReferencePlane> refPlanes in refPlanesCeilings)
            {
                foreach (ReferencePlane refPlane in refPlanes)
                {
                    doc.Delete(refPlane.Id);
                }
            }
        }//script 3
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
            //ElementType ceilingType = doc.GetElement(typeId) as ElementType;
            string ceilingType = ceiling.LookupParameter("Type Comments").AsString();
            //type = ceilingType?.Name ?? "Unknown Type";
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