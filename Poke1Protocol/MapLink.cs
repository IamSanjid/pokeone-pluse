using Resp = MAPAPI.Response;
using System;

namespace Poke1Protocol
{
    public class MapLink
    {
        public int DestinationX;
        public int DestinationY;

        public bool IsVisible { get; private set; }

        public Resp.LINKData Data { get; private set; }

        public Guid Id { get; private set; }

        public Guid DestinationId { get; private set; }

        public MapLink(int x, int y, Guid id, Guid destinationId)
        {
            DestinationX = x;
            DestinationY = y;
            IsVisible = true;
            Id = id;
            DestinationId = destinationId;
        }

        public MapLink(Resp.LINKData data)
        {
            Data = data;
            DestinationX = data.x;
            DestinationY = -data.z;
            IsVisible = data.DestinationID != Guid.Empty;
            Id = data.ID;
            DestinationId = data.DestinationID;
        }

        public void SetVisibility(bool hide)
            => IsVisible = !hide;

    }
}
