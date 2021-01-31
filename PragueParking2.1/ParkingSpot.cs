using System;
using System.Collections.Generic;
using System.Text;

namespace PragueParking2._1
{
    public class ParkingSpot
    {
        public ParkingSpot(int parkingSpotNumber, int size)
        {
            this.FreeSpace = size;
            this.ParkingSpotNumber = parkingSpotNumber;
        }
        public void UseParking(int vehicleSize, Vehicle vehicle)
        {
            if (this.FreeSpace >= vehicleSize)
            {
                this.FreeSpace = this.FreeSpace - vehicleSize;
                ParkedVehiclesOnSpot.Add(vehicle);
            }
        }
        public List<Vehicle> ListParkedVehicles()
        {
            return ParkedVehiclesOnSpot;
        }

        public int findIndexOfVehicle(string tokenOrRegistrationNumber)
        {
            int idx = 0;
            foreach (Vehicle vehicle in ParkedVehiclesOnSpot)
            {
                if (vehicle.RegistrationNumber == tokenOrRegistrationNumber || vehicle.Token == tokenOrRegistrationNumber)
                {
                    return idx;
                }
                idx++;
            }
            return -1;
        }
        public bool checkForFreeSpace(int vehicleSize)
        {
            if (vehicleSize <= this.FreeSpace)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public void removeVehicle(int vehicleIndex)
        {
            int vehicleSize = getVehicleSize(this.ParkedVehiclesOnSpot[vehicleIndex]);
            this.ParkedVehiclesOnSpot.RemoveAt(vehicleIndex);
            this.FreeSpace = this.FreeSpace + vehicleSize;
        }
        public int getVehicleSize(Vehicle vehicle)
        {
            int sizeOfVehicle = -1;
            if (vehicle.GetType().Equals(typeof(Bicycle)))
            {
                sizeOfVehicle = Program.config.BicycleSize;
            }
            if (vehicle.GetType().Equals(typeof(Mc)))
            {
                sizeOfVehicle = Program.config.McSize;
            }
            if (vehicle.GetType().Equals(typeof(Car)))
            {
                sizeOfVehicle = Program.config.CarSize;
            }
            if (vehicle.GetType().Equals(typeof(Bus)))
            {
                sizeOfVehicle = Program.config.BusSize;
            }

            return sizeOfVehicle;
        }
        public int getVehiclePrice(Vehicle vehicle)
        {
            int pricePerHour = 0;
            if (vehicle.GetType().Equals(typeof(Bicycle)))
            {
                pricePerHour = Program.config.BicyclePrice;
            }
            if (vehicle.GetType().Equals(typeof(Mc)))
            {
                pricePerHour = Program.config.McPrice;
            }
            if (vehicle.GetType().Equals(typeof(Car)))
            {
                pricePerHour = Program.config.CarPrice;
            }
            if (vehicle.GetType().Equals(typeof(Bus)))
            {
                pricePerHour = Program.config.BusPrice;
            }
            return pricePerHour;
        }
        public float calculateCharge(int indexToCalculate)
        {
            int pricePerHour = getVehiclePrice(ParkedVehiclesOnSpot[indexToCalculate]);
            var expiredMinutes = (DateTime.Now - ParkedVehiclesOnSpot[indexToCalculate].ParkedSince).TotalMinutes;
            if (expiredMinutes < 10)
            {
                return 0;
            }
            else
            {
                float totalPrice = (int)expiredMinutes * pricePerHour / 60;
                return totalPrice;
            }
        }

        public float deParkVehicleFromSpot(int indexToRemove, Config config)
        {
            int sizeOfDeparkedVehicle = getVehicleSize(ParkedVehiclesOnSpot[indexToRemove]);
            if(ParkedVehiclesOnSpot[indexToRemove] is Bus)
            {
                sizeOfDeparkedVehicle /= config.ParkingSpotSize;
            }
            int pricePerHour = getVehiclePrice(ParkedVehiclesOnSpot[indexToRemove]);
            var expiredMinutes = (DateTime.Now - ParkedVehiclesOnSpot[indexToRemove].ParkedSince).TotalMinutes;
            ParkedVehiclesOnSpot.RemoveAt(indexToRemove);
            this.FreeSpace = this.FreeSpace + sizeOfDeparkedVehicle;
            if (expiredMinutes < 10)
            {
                return 0;
            }
            else
            {
                float totalPrice = (int)expiredMinutes * pricePerHour / 60;
                return totalPrice;
            }
        }
        public int FreeSpace { get; set; }
        public int ParkingSpotNumber { get; set; }
        public List<Vehicle> ParkedVehiclesOnSpot = new List<Vehicle>();
    }
}
