namespace Ro_Sys_Test.Classes
{
    public class Cell
    {
        public int Value { get; set; }
        public double AvarageNeighborValue { get; set; }
        public int Col { get; set; }
        public int Row { get; set; }
        public double Longtitude { get; set; }
        public double Latitude { get; set; }
        public Rating Cluster { get; set; }

        public (double x, double y) ToVector()
        {
            return (Value, AvarageNeighborValue);
        }
    }
}
