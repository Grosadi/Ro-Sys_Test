// See https://aka.ms/new-console-template for more information
using ClipperLib;
using OpenCvSharp;
using Ro_Sys_Test;
using Ro_Sys_Test.Classes;
using System.Text.Json;

var connectionString = ConfigurationHelper.GetConnectionString("RoSysConnection");
const string sqlQuery = "SELECT \r\n\t(gv).val as value,\r\n\t(gv).x as col,\r\n\t(gv).y as row,\r\n\tst_x((gv).geom) as longtitude,\r\n\tst_y((gv).geom) as latitude\r\nFROM (\r\n\tselect st_pixelaspoints(geom, 1) as gv\r\n\tFROM public.grids\r\n\t) as g";

const int clusterCount = 3;
const int minArea = 5000;

var cells = await DatabaseHelper.LoadData(connectionString, sqlQuery);

if (cells == null || cells.Count == 0)
{
    Console.WriteLine("Empty dataset!");
    return;
}

Console.WriteLine("Generating value matrix...");
var matrix = FillMatrixWithValues(cells);


Console.WriteLine("Calculate avg neighbor values...");
CalculateAvgNeighborValues(matrix, cells);


Console.WriteLine("Applying K-Means algorithm...");
var labels = KMeans(cells.Select(c => c.ToVector()).ToList(), clusterCount);


Console.WriteLine("Set clusters...");
SetClusters(labels, cells);


Console.WriteLine("Generate polygons...");
var polygons = GeneratePolygons(cells);
Console.WriteLine($"Generated polygons: {polygons.Count}");


Console.WriteLine("Unify small polygons...");
var unifiedPolygons = UnifyPolygons(polygons, minArea);
Console.WriteLine($"Unified polygons: {unifiedPolygons.Count}");


Console.WriteLine("Eliminate small polygons...");
var largePolygons = unifiedPolygons.Where(up => up.GetArea() >= minArea).ToList();
Console.WriteLine($"Large polygons: {largePolygons.Count}");


Console.WriteLine("Assign points to cells...");
AssignPointsToCells(largePolygons, cells);


Console.WriteLine("Merging into multipolygons...");
var multiPolygons = MergeIntoMultiPolygons(largePolygons);


foreach (var mp in multiPolygons.OrderBy(m => m.Key))
{
    Console.WriteLine($"Cluster: {mp.Key} - Avg Value: {mp.Value.Average(p => p.GetAvgValue())}");
}


Console.WriteLine("Export to geojson...");
string geojson = ConvertMultiPolygonsToGeoJson(multiPolygons);
File.WriteAllText("output.geojson", geojson);


#region Methods

static int[,] FillMatrixWithValues(List<Cell> cells)
{
    var matrix = new int[cells.Max(c => c.Row), cells.Max(c => c.Col)];

    foreach (var cell in cells)
    {
        int x = cell.Row - 1;
        int y = cell.Col - 1;

        matrix[x, y] = cell.Value;
    }    

    return matrix;
}


static void CalculateAvgNeighborValues(int[,] matrix, List<Cell> cells)
{
    foreach (var cell in cells)
    {
        cell.AvarageNeighborValue = GetNeighborAvg(matrix, matrix.GetLength(0), matrix.GetLength(1), cell.Row - 1, cell.Col - 1);
    }    
}


static double GetNeighborAvg(int[,] matrix, int rows, int cols, int x, int y)
{
    int[] dx = { -1, 0, 1, 0 };
    int[] dy = { 0, -1, 0, 0 };

    int sum = 0;
    int count = 0;

    for (int i = 0; i < 4; i++) // 4 neighbors only
    {
        int nx = x + dx[i];
        int ny = y + dy[i];

        if (nx >= 0 && nx < rows && ny >= 0 && ny < cols)
        {
            sum += matrix[nx, ny];
            count++;
        }
    }

    return count > 0 ? sum / count : 0;
}


static int[] KMeans(List<(double x, double y)> data, int k, int maxIterations = 100)
{
    Random rnd = new Random();
    var centroids = data.OrderBy(x => rnd.Next()).Take(k).ToArray();
    int[] labels = new int[data.Count];

    for (int iteration = 0; iteration < maxIterations; iteration++)
    {        
        for (int i = 0; i < data.Count; i++)
        {
            double minDist = double.MaxValue;
            int bestCluster = 0;

            for (int c = 0; c < k; c++)
            {
                double dist = Distance(data[i], centroids[c]);
                if (dist < minDist)
                {
                    minDist = dist;
                    bestCluster = c;
                }
            }

            labels[i] = bestCluster;
        }

        var newCentroids = new (double x, double y)[k];
        var counts = new int[k];

        for (int i = 0; i < data.Count; i++)
        {
            int label = labels[i];
            newCentroids[label].x += data[i].x;
            newCentroids[label].y += data[i].y;
            counts[label]++;
        }

        for (int c = 0; c < k; c++)
        {
            if (counts[c] > 0)
            {
                centroids[c] = (newCentroids[c].x / counts[c], newCentroids[c].y / counts[c]);
            }
        }        
    }

    return labels;

    double Distance((double x, double y) a, (double x, double y) b) =>
        Math.Sqrt(Math.Pow(a.x - b.x, 2) + Math.Pow(a.y - b.y, 2));
}


static void SetClusters(int[] labels, List<Cell> cells)
{
    for (int i = 0; i < cells.Count; i++)
    {
        cells[i].Cluster = (Rating)labels[i];
    }
}


static List<Polygon> GeneratePolygons(List<Cell> uniqueCells)
{
    int rows = uniqueCells.Max(c => c.Row);
    int cols = uniqueCells.Max(c => c.Col);

    var polygons = new List<Polygon>();

    var uniqueClasses = new HashSet<int>();

    foreach (var cell in uniqueCells)
    {
        uniqueClasses.Add((int)cell.Cluster);
    }

    foreach (var classId in uniqueClasses)
    {
        Mat binary = new Mat(rows, cols, MatType.CV_8UC1);

        foreach (var cell in uniqueCells)
        {
            byte value = (byte)((int)cell.Cluster == classId ? 255 : 0);
            binary.Set(cell.Row - 1, cell.Col - 1, value);
        }

        Cv2.FindContours(binary, out Point[][] contours, out HierarchyIndex[] _, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

        foreach (var contour in contours)
        {
            var points = new List<(int x, int y)>();            

            foreach (var pt in contour)
            {
                points.Add((pt.X, pt.Y));                
            }

            var polygon = new Polygon(points, (Rating)classId);            

            polygons.Add(polygon);
        }
    }

    return polygons;
}


static List<Polygon> UnifyPolygons(List<Polygon> polygons, int minArea)
{    
    var result = new List<Polygon>();
    var small = new List<Polygon>();
    var large = new List<Polygon>();    

    foreach (var poly in polygons)
    {
        double area = Math.Abs(Clipper.Area(ToPath(poly.Points)));

        if (area < minArea)
            small.Add(poly);
        else
            large.Add(poly);
    }

    foreach (var sp in small)
    {
        Polygon closest = null;
        double minDist = double.MaxValue;

        foreach (var largePolly in large.Where(p => p.Cluster == sp.Cluster))
        {
            foreach (var (x, y) in sp.Points)
            {
                foreach (var lp in largePolly.Points)
                {
                    double dist = Math.Sqrt(Math.Pow(x - lp.x, 2) + Math.Pow(y - lp.y, 2));
                    if (dist < minDist)
                    {
                        minDist = dist;
                        closest = largePolly;
                    }
                }
            }
        }

        if (closest != null)
        {
            var clipper = new Clipper();
            var subj = ToPath(closest.Points);
            var clip = ToPath(sp.Points);

            clipper.AddPath(subj, PolyType.ptSubject, true);
            clipper.AddPath(clip, PolyType.ptClip, true);

            var solution = new List<List<IntPoint>>();
            clipper.Execute(ClipType.ctUnion, solution, PolyFillType.pftNonZero, PolyFillType.pftNonZero);

            large.Remove(closest);

            foreach (var unionPath in solution)
            {
                large.Add(new Polygon
                {
                    Cluster = sp.Cluster,
                    Points = unionPath.Select(p => (p.X, p.Y)).ToList()
                });
            }
        }
        else
        {
            result.Add(sp);
        }
    }

    result.AddRange(large);
    return result;

    List<IntPoint> ToPath(List<(int x, int y)> points) =>
    points.Select(p => new IntPoint(p.x, p.y)).ToList();
}


static void AssignPointsToCells(List<Polygon> largePolygons, List<Cell> cells)
{   
    foreach (var poly in largePolygons)
    {
        foreach (var (x, y) in poly.Points)
        {
            var cell = cells.FirstOrDefault(c => c.Row - 1 == x && c.Col - 1 == y);

            if (cell != null) poly.Cells.Add(cell);
        }
    }
}


static Dictionary<int, List<Polygon>> MergeIntoMultiPolygons(List<Polygon> largePolygons)
{
    var multiPolygons = new Dictionary<int, List<Polygon>>();

    foreach (var poly in largePolygons)
    {
        int key = (int)poly.Cluster;

        if (!multiPolygons.ContainsKey(key))
        {
            multiPolygons[key] = new List<Polygon>();
        }

        multiPolygons[key].Add(poly);
    }

    return multiPolygons;
}


static string ConvertMultiPolygonsToGeoJson(Dictionary<int, List<Polygon>> multiPolygons)
{
    var features = new List<GeoJsonFeature>();

    foreach (var multiPoly in multiPolygons)
    {
        var cluster = multiPoly.Value.FirstOrDefault().Cluster;
        var avgValue = multiPoly.Value.Average(mp => mp.GetAvgValue());

        var coords = new List<List<List<double>>>();

        foreach (var poly in multiPoly.Value)
        {
            var polyCoords = poly.Cells.Select(c => new List<double> { c.Latitude, c.Longtitude }).ToList();
            coords.Add(polyCoords);

            if (!coords[0].SequenceEqual(coords[^1])) coords.Add(coords[0]);            
        }

        var feature = new GeoJsonFeature
        {
            Geometry = new GeoJsonGeometry
            {
                Coordinates = new List<List<List<List<double>>>> { coords }
            },
            Properties = new Dictionary<string, object>
            {
                { "class", cluster },
                { "avg-value", avgValue },
            }
        };

        features.Add(feature);
    }

    var geoJson = new
    {
        type = "FeatureCollection",
        features = features
    };

    return JsonSerializer.Serialize(geoJson, new JsonSerializerOptions { WriteIndented = true });
}

#endregion