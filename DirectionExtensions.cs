using System;
using UnityEngine;

namespace WaveFunction
{
    public static class DirectionExtensions
    {
        private const byte Mask = 0b00000111;

        public static Direction Opposite(this Direction direction)
        {
            return (Direction)(~(byte)direction & Mask);
        }

        public static Direction ClockWiseTurn(this Direction direction)
        {
            return direction switch
            {
                Direction.Left => Direction.Front,
                Direction.Front => Direction.Right,
                Direction.Right => Direction.Back,
                Direction.Back => Direction.Left,
                _ => direction
            };
        }
        
        public static Direction CounterClockWiseTurn(this Direction direction)
        {
            return direction switch
            {
                 Direction.Front => Direction.Left,
                 Direction.Right => Direction.Front,
                 Direction.Back => Direction.Right,
                 Direction.Left => Direction.Back,
                _ => direction
            };
        }

        public static Vector3Int ToVector3(this Direction direction)
        {
            return direction switch
            {
                Direction.Left => Vector3Int.left,
                Direction.Right => Vector3Int.right,
                Direction.Front => Vector3Int.forward,
                Direction.Back => Vector3Int.back,
                Direction.Up => Vector3Int.up,
                Direction.Down => Vector3Int.down,
                _ => throw new ArgumentOutOfRangeException(nameof(direction))
            };
        }
    }
}