using System;
using System.Collections.Generic;
using System.Text;

namespace PragueParking2._1
{
    public class Car : Vehicle
    {
        public Car(string registrationNumber, string owner, string brand, string model, int parkedAt, DateTime parkedSince) : base(registrationNumber, owner, parkedAt, parkedSince)
        {
            this.Brand = brand;
            this.Model = model;
        }
        public Car(string registrationNumber, string owner, string brand, string model, int parkedAt, DateTime parkedSince, string token) : base(registrationNumber, owner, parkedAt, parkedSince, token)
        {
            this.Brand = brand;
            this.Model = model;
        }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string identifier { get { return "car"; } }
    }
}
