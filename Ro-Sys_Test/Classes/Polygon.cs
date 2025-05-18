using ClipperLib;

namespace Ro_Sys_Test.Classes
{
    public class Polygon
    {
        public Polygon()
        {
            Points = new List<(int x, int y)>();
            Cells = new List<Cell>();           
        }

        public Polygon(List<(int x, int y)> points, Rating cluster)
        {
            Points = points;
            Cells = new List<Cell>();
            Cluster = cluster;            
        }

        public Rating Cluster { get; set; }        
        public List<(int x, int y)> Points;
        public List<Cell> Cells;        

        public double GetArea()
        {
            return Clipper.Area(ToPath(Points));
        }

        public double GetAvgValue()
        {
            return Cells.Average(c => c.Value);
        }

       private List<IntPoint> ToPath(List<(int x, int y)> points) =>
        points.Select(p => new IntPoint(p.x, p.y)).ToList();
    }
}
