﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace StroMoHab_Objects.Graphics
{
    interface ICompoundObject
    {
        List<BoundingBox> listOfCollisionModels();
    }
}
