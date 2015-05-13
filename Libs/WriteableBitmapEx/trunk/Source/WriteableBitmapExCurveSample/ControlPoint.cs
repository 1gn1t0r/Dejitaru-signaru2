﻿#region Header
//
//   Project:           WriteableBitmapEx - Silverlight WriteableBitmap extensions
//   Description:       A control point for a spline curve.
//
//   Changed by:        $Author$
//   Changed on:        $Date$
//   Changed in:        $Revision$
//   Project:           $URL$
//   Id:                $Id$
//
//
//   Copyright © 2009-2015 Rene Schulte and WriteableBitmapEx Contributors
//
//   This code is open source. Please read the License.txt for details. No worries, we won't sue you! ;)
//
#endregion


using System.Windows;
using System;
using Schulte.Silverlight;

#if NETFX_CORE
using Windows.Foundation;
#endif

using Vector = Schulte.Silverlight.Vector;

namespace WriteableBitmapExCurveSample
{
   /// <summary>
   /// A control point for a spline curve.
   /// </summary>
   public class ControlPoint
   {
      private Vector point;      
      
      public int X { get { return point.X; } set { point.X = value; } }
      public int Y { get { return point.Y; } set { point.Y = value; } }
      

      public ControlPoint(Vector point)
      {
         this.point = point;
      }

      public ControlPoint()
         : this(Vector.Zero)
      {
      }

      public ControlPoint(int x, int y)
         : this(new Vector(x, y))
      {
      }

      public ControlPoint(Point point)
         : this((int)point.X, (int) point.Y)
      {
      }

      public override string ToString()
      {
         return String.Format("({0}, {1})", X, Y);;
      }
   }
}