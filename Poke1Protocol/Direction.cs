using System.Drawing;

namespace Poke1Protocol
{
    public enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }
    public static class DirectionExtensions
    {
        public static string AsChar(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return "u";
                case Direction.Down:
                    return "d";
                case Direction.Left:
                    return "l";
                case Direction.Right:
                    return "r";
            }
            return null;
        }

        public static Direction GetOpposite(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return Direction.Down;
                case Direction.Down:
                    return Direction.Up;
                case Direction.Left:
                    return Direction.Right;
                case Direction.Right:
                default:
                    return Direction.Left;
            }
        }

        public static void ApplyToCoordinates(this Direction direction, ref int x, ref int y)
        {
            switch (direction)
            {
                case Direction.Up:
                    y--;
                    break;
                case Direction.Down:
                    y++;
                    break;
                case Direction.Left:
                    x--;
                    break;
                case Direction.Right:
                    x++;
                    break;
            }
        }
        /// <summary>
		/// Generates the next point in moving direction.
		/// </summary>
		/// <param name="direction">The moving direction.</param>
		/// <param name="origin">The starting point.</param>
		/// <returns>New point after movement in direction was applied.</returns>
		//public static Point ApplyToCoordinates(this Direction direction, Point origin)
  //      {
  //          int x = origin.X, y = origin.Y;
  //          switch (direction)
  //          {
  //              case Direction.Up:
  //                  y--;
  //                  break;
  //              case Direction.Down:
  //                  y++;
  //                  break;
  //              case Direction.Left:
  //                  x--;
  //                  break;
  //              case Direction.Right:
  //                  x++;
  //                  break;
  //          }

  //          return new Point(x, y);
  //      }


        public static Direction FromChar(char c)
        {
            switch (c)
            {
                case 'u':
                    return Direction.Up;
                case 'd':
                    return Direction.Down;
                case 'l':
                    return Direction.Left;
                case 'r':
                    return Direction.Right;
            }
            throw new System.Exception("The direction '" + c + "' does not exist");
        }

        /// <summary>
        /// Converts an integer into an actual direction. Needed for NPC view directions, but could also be used elsewhere.
        /// </summary>
        /// <param name="direction">Integer representing an npc's view direction.</param>
        /// <returns>The converted direction.</returns>
        public static Direction FromInt(int direction)
        {
            switch (direction)
            {
                case 0:
                    return Direction.Up;
                case 1:
                    return Direction.Right;
                case 2:
                    return Direction.Down;
                case 3:
                    return Direction.Left;
            }
            throw new System.Exception("The direction '" + direction + "' does not exist");
        }

        public static PSXAPI.Request.MoveAction[] ToMoveActions(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Down:
                    return new[] { PSXAPI.Request.MoveAction.Down };
                case Direction.Up:
                    return new[] { PSXAPI.Request.MoveAction.Up };
                case Direction.Right:
                    return new[] { PSXAPI.Request.MoveAction.Right };
                case Direction.Left:
                    return new[] { PSXAPI.Request.MoveAction.Left };
            }
            throw new System.Exception("The direction '" + direction + "' does not exist");
        }

        public static Direction FromPlayerDirectionResponse(PSXAPI.Response.PlayerDirection direction)
        {
            switch (direction)
            {
                case PSXAPI.Response.PlayerDirection.Default:
                    return Direction.Down;
                case PSXAPI.Response.PlayerDirection.Down:
                    return Direction.Down;
                case PSXAPI.Response.PlayerDirection.Left:
                    return Direction.Left;
                case PSXAPI.Response.PlayerDirection.Right:
                    return Direction.Right;
                case PSXAPI.Response.PlayerDirection.Up:
                    return Direction.Up;
            }
            throw new System.Exception("The direction '" + direction + "' does not exist");
        }

        public static PSXAPI.Request.MoveAction ToOneStepMoveActions(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Down:
                    return PSXAPI.Request.MoveAction.TurnDown;
                case Direction.Up:
                    return PSXAPI.Request.MoveAction.TurnUp;
                case Direction.Right:
                    return PSXAPI.Request.MoveAction.TurnRight;
                case Direction.Left:
                    return PSXAPI.Request.MoveAction.TurnLeft;
            }
            throw new System.Exception("The direction '" + direction + "' does not exist");
        }
    }
}
