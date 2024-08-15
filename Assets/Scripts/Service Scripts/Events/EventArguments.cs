using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Common
{
    public class KeyPressedEventArgs : EventArgs
    {
        public KeyCode KeyCode { get; }

        public KeyPressedEventArgs(KeyCode keyCode)
        {
            KeyCode = keyCode;
        }
    }

    public class ShipEventArgs : EventArgs
    {
        public ShipBase Ship { get; }

        public ShipEventArgs(ShipBase ship)
        {
            Ship = ship;
        }
    }
}
