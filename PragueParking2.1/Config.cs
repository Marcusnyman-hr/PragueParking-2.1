using System;
using System.Collections.Generic;
using System.Text;

namespace PragueParking2._1
{
    public class Config
    {
        public Config(int parkingSpotsAmount, int parkingSpotSize, int bicycleSize, int mcSize, int carSize, int busSize, int bicyclePrice, int mcPrice, int carPrice, int busPrice, List<dynamic> vehicleTypes)
        {
            this.VehicleTypes = vehicleTypes;
            this.ParkingSpotsAmount = parkingSpotsAmount;
            this.ParkingSpotSize = parkingSpotSize;
            this.BicycleSize = bicycleSize;
            this.McSize = mcSize;
            this.CarSize = carSize;
            this.BusSize = busSize;
            this.BicyclePrice = bicyclePrice;
            this.McPrice = mcPrice;
            this.CarPrice = carPrice;
            this.BusPrice = busPrice;
        }
        public List<dynamic> VehicleTypes { get; set; }
        public int ParkingSpotsAmount { get; set; }
        public int ParkingSpotSize { get; set; }
        public int BicycleSize { get; set; }
        public int McSize { get; set; }
        public int CarSize { get; set; }
        public int BusSize { get; set; }
        public int BicyclePrice { get; set; }
        public int McPrice { get; set; }
        public int CarPrice { get; set; }
        public int BusPrice { get; set; }
    }
}
