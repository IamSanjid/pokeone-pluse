namespace Poke1Protocol
{
    public class AreaLink
    {
        public string DestinationArea;
        public int DestinationX;
        public int DestinationY;

        public AreaLink(string area, int x, int y)
        {
            DestinationArea = area;
            DestinationX = x;
            DestinationY = y;
        }
    }
}
