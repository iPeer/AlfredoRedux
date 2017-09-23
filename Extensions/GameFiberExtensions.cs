﻿using Rage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlfredoRedux.Extensions
{
    static class GameFiberExtensions
    {
        internal static void Resume(this GameFiber fiber)
        {
            if (fiber != null && !fiber.IsAlive)
            {
                if (fiber.IsHibernating) fiber.Wake();
                else fiber.Start();
            }
        }
        internal static bool IsRunning(this GameFiber fiber)
        {
            return fiber.IsAlive && !fiber.IsHibernating;
        }
    }
}
