using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PragueParking2._1
{
    public class Vehicle
    {
        public Vehicle() : this("undefined", "undefined", 999, DateTime.Now)
        {
        }
        public Vehicle(string owner)
        {
            this.Owner = owner;
        }
        public Vehicle(string owner, DateTime parkedSince)
        {
            this.Owner = owner;
            this.ParkedSince = parkedSince;
            this.Token = GenerateToken();
        }
        public Vehicle(string owner, int parkedAt, DateTime parkedSince)
        {
            this.Owner = owner;
            this.ParkedAt = parkedAt;
            this.ParkedSince = parkedSince;
            this.Token = GenerateToken();
        }
        public Vehicle(string registrationNumber, int[] parkedAtSeveral, DateTime parkedSince)
        {
            this.RegistrationNumber = registrationNumber;
            this.ParkedAtSeveral = parkedAtSeveral;
            this.ParkedSince = parkedSince;
            this.Token = GenerateToken();
        }
        public Vehicle(string registrationNumber, int[] parkedAtSeveral, DateTime parkedSince, string token)
        {
            this.RegistrationNumber = registrationNumber;
            this.ParkedAtSeveral = parkedAtSeveral;
            this.ParkedSince = parkedSince;
            this.Token = token;
        }
        public Vehicle(string owner, int parkedAt, DateTime parkedSince, string token)
        {
            this.Owner = owner;
            this.ParkedAt = parkedAt;
            this.ParkedSince = parkedSince;
            this.Token = token;
        }
        public Vehicle(string registrationNumber, string owner, int parkedAt, DateTime parkedSince)
        {
            this.RegistrationNumber = registrationNumber;
            this.Owner = owner;
            this.ParkedAt = parkedAt;
            this.ParkedSince = parkedSince;
            this.Token = GenerateToken();
        }
        public Vehicle(string registrationNumber, string owner, int parkedAt, DateTime parkedSince, string token)
        {
            this.RegistrationNumber = registrationNumber;
            this.Owner = owner;
            this.ParkedAt = parkedAt;
            this.ParkedSince = parkedSince;
            this.Token = token;
        }
        private string GenerateToken()
        {
            const string availableCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var newToken = new String(Enumerable.Repeat(availableCharacters, 10).Select(s => s[random.Next(s.Length)]).ToArray());
            return newToken;
        }

        public string RegistrationNumber { get; set; }
        public string Owner { get; set; }
        public int ParkedAt { get; set; }
        public int[] ParkedAtSeveral { get; set; }
        public DateTime ParkedSince { get; set; }
        public string Token { get; }

    }
}
