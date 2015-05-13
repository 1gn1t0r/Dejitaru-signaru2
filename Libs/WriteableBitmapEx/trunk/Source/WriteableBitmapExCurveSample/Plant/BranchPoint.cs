#region Header
//
//   Project:           Silverlight procedural Plant
//
//   Changed by:        $Author$
//   Changed on:        $Date$
//   Changed in:        $Revision$
//   Project:           $URL$
//   Id:                $Id$
//
//
//   Copyright © 2010-2015 Rene Schulte and WriteableBitmapEx Contributors
//
//   This code is open source. Please read the License.txt for details. No worries, we won't sue you! ;)
//
#endregion

using System.Collections.Generic;
using System;

namespace Schulte.Silverlight
{
   /// <summary>
   /// A branching point of a plant.
   /// </summary>
   public struct BranchPoint
   {
      public float Time;
      public int Angle;

      public BranchPoint(float time, int angle)
      {
         this.Time = time;
         this.Angle = angle;
      }
   }
}
