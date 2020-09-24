using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RenumberParts
{
    class ConnectorsHelper
    {

        public static bool ConnStatus(Connector con)
        {
            bool result;

            if (con.IsConnected && con.ConnectorType.ToString() != "Curve")
            {
                result = true;
            }
            else
            {
                result = false;
            }
            return result;
        }

        //Check if the current element is a Tee
        public static bool TeeDetect(ConnectorSet Connectors)
        {
            bool result;

            int tempInt = 0;

            foreach (Connector con in Connectors)
            {
                if (con.ConnectorType.ToString() != "Curve")
                {
                    tempInt += 1;
                }
            }

            if(tempInt == 3)
            {
                result = true;
            }
            else
            {
                result = false;
            }

            return result;
        }


    }
}
