﻿using System;
using System.Collections.Generic;
using System.Text;

namespace PragueParking2._1
{
    public class Bus : Vehicle
    {
        public Bus(string registrationNumber, int[] parkedAtSeveral, DateTime parkedSince) : base(registrationNumber, parkedAtSeveral, parkedSince)
        {
        }
        public Bus(string registrationNumber, int[] parkedAtSeveral, DateTime parkedSince, string token) : base(registrationNumber, parkedAtSeveral, parkedSince, token)
        {
        }
        public string identifier { get { return "bus"; } }
    }
}
