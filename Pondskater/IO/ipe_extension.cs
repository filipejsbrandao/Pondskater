using System;
using Rhino;
using Rhino.Geometry;

namespace Pondskater.IO
{
    /*public partial class Ipe
    {
        public Ipe()
        {
        }

    }

    public partial class IpeInfo
    {
        public IpeInfo()
        {

        }
    }

    public partial class IpeIpestyle
    {
        public IpeIpestyle()
        {

        }
    }

    public partial class IpeIpestylePen
    {
        public IpeIpestylePen()
        {

        }
    }

    public partial class IpeIpestyleColor
    {
        public IpeIpestyleColor()
        {

        }
    }

    public partial class IpeIpestyleDashstyle
    {
        public IpeIpestyleDashstyle()
        {

        }
    }

    public partial class IpePage
    {
        public IpePage()
        {

        }
    }

    public partial class IpePageLayer
    {
        public IpePageLayer()
        {

        }
    }

    public partial class IpePageGroup
    {
        public IpePageGroup()
        {

        }
    }*/

    public partial class ipePageGroupGroupPath
    {
        public ipePageGroupGroupPath()
        {

        }

        /// <summary>
        /// Converts a ipe path into a Rhino line
        /// </summary>
        /// <returns>returns a rhino line or an exception if one of the points can not be parsed</returns>
        public Line PathToRHLine()
        {
            string[] line = this.Value.Split(new string[] { "\r\n", "\r", "\n", " " }, StringSplitOptions.RemoveEmptyEntries);
            try
            {
                double xFrom = Double.Parse(line[0]);
                double yFrom = Double.Parse(line[1]);
                double xTo = Double.Parse(line[3]);
                double yTo = Double.Parse(line[4]);
                return new Line(xFrom, yFrom, 0.0, xTo, yTo, 0.0);

            }
            catch(ArgumentException e)
            {
                throw new ArgumentException("cannot parse the ipe string values into doubles", e);
            }
            catch (FormatException e)
            {
                throw new ArgumentException("the format of the ipe string values is not recognized", e);
            }
            catch (OverflowException e)
            {
                throw new ArgumentException("one of the points in the ipe string either too large or too small", e);
            }
        }

        
    }
    public partial class ipePageGroupPath
    {
        public ipePageGroupPath()
        {

        }

        public Line PathToRHLine()
        {
            string[] line = this.Value.Split(new string[] { "\r\n", "\r", "\n", " " }, StringSplitOptions.RemoveEmptyEntries);
            try
            {
                double xFrom = Double.Parse(line[0]);
                double yFrom = Double.Parse(line[1]);
                double xTo = Double.Parse(line[3]);
                double yTo = Double.Parse(line[4]);
                return new Line(xFrom, yFrom, 0.0, xTo, yTo, 0.0);

            }
            catch (ArgumentException e)
            {
                throw new ArgumentException("cannot parse the ipe string values into doubles", e);
            }
            catch (FormatException e)
            {
                throw new ArgumentException("the format of the ipe string values is not recognized", e);
            }
            catch (OverflowException e)
            {
                throw new ArgumentException("one of the points in the ipe string either too large or too small", e);
            }

        }
    }
}
