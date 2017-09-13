using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TrainFinderBot.Models
{
    public class Station
    {
        public string code { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Yomi { get; set; }
    }

    public class Prefecture
    {
        public string code { get; set; }
        public string Name { get; set; }
    }

    public class GeoPoint
    {
        public string longi { get; set; }
        public string lati { get; set; }
        public string longi_d { get; set; }
        public string lati_d { get; set; }
        public string gcs { get; set; }
    }

    public class Point
    {
        public Station Station { get; set; }
        public Prefecture Prefecture { get; set; }
        public GeoPoint GeoPoint { get; set; }
    }

    public class ResultSet
    {
        public string apiVersion { get; set; }
        public string engineVersion { get; set; }
        public List<Point> Point { get; set; }
    }

    public class StationModel
    {
        public ResultSet ResultSet { get; set; }
    }


}